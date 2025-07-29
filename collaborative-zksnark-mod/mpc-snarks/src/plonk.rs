use ark_ff::UniformRand;
use ark_poly::univariate::DensePolynomial;
use ark_poly_commit::marlin_pc::MarlinKZG10;
use ark_std::{end_timer, start_timer, test_rng};
use mpc_algebra::honest_but_curious::*;
use mpc_algebra::Reveal;
use mpc_plonk::*;
use std::collections::HashMap;
use structured::PlonkCircuit;

use serde::Deserialize;
use serde_json::Result;
use std::fs::File;
use std::io::Read;

type F = ark_bls12_377::Fr;
type E = ark_bls12_377::Bls12_377;
type ME = MpcPairingEngine<ark_bls12_377::Bls12_377>;
type MF = MpcField<F>;
type MpcMarlinKZG10 = MarlinKZG10<ME, DensePolynomial<MF>>;
type LocalMarlinKZG10 = MarlinKZG10<E, DensePolynomial<F>>;
type LocalPlonk = mpc_plonk::Plonk<F, LocalMarlinKZG10>;
type MpcPlonk = mpc_plonk::Plonk<MF, MpcMarlinKZG10>;

pub fn local_test_prove_and_verify(n_iters: usize) {
    use relations::{flat::*, structured::*};
    let steps = n_iters;
    let start = F::from(2u64);
    let c = PlonkCircuit::<F>::new_squaring_circuit(steps, Some(start));
    let res = (0..steps).fold(start, |a, _| a * a);
    let public: HashMap<String, F> = vec![("out".to_owned(), res)].into_iter().collect();
    let circ = CircuitLayout::from_circuit(&c);

    let setup_rng = &mut test_rng();
    let zk_rng = &mut test_rng();

    let v_circ = {
        let mut t = circ.clone();
        t.p = None;
        t
    };

    let srs = LocalPlonk::universal_setup(steps.next_power_of_two(), setup_rng);
    let (pk, vk) = LocalPlonk::circuit_setup(&srs, &v_circ);
    let pf = LocalPlonk::prove(&pk, &circ, zk_rng);
    LocalPlonk::verify(&vk, &v_circ, pf, &public);
}

pub fn mpc_test_prove_and_verify(n_iters: usize) {
    use relations::{flat::*, structured::*};
    let steps = n_iters;

    // empty circuit
    let v_c = PlonkCircuit::<F>::new_squaring_circuit(steps, None);
    let v_circ = CircuitLayout::from_circuit(&v_c);
    // setup
    let setup_rng = &mut test_rng();
    let srs = LocalPlonk::universal_setup(steps.next_power_of_two(), setup_rng);
    let (pk, vk) = LocalPlonk::circuit_setup(&srs, &v_circ);

    // data circuit
    let data_rng = &mut test_rng();
    let start = MF::rand(data_rng);
    let res = (0..steps).fold(start, |a, _| a * a);
    let public: HashMap<String, F> = vec![("out".to_owned(), res.reveal())].into_iter().collect();
    let c = PlonkCircuit::<MF>::new_squaring_circuit(steps, Some(start));
    let circ = CircuitLayout::from_circuit(&c);

    let t = start_timer!(|| "timed section");
    let mpc_pk = ProverKey::from_public(pk);
    let mpc_pf = MpcPlonk::prove(&mpc_pk, &circ, &mut test_rng());
    let pf = mpc_pf.reveal();
    end_timer!(t);
    LocalPlonk::verify(&vk, &v_circ, pf, &public);
}

#[derive(Debug, Deserialize)]
struct WireValue {
    value: String,
    is_secret_share: bool,
}

#[derive(Debug, Deserialize)]
struct CompatCircuitR1csJson {
    wire_values: Vec<WireValue>,
    wire_count: u32,
    public_wire_count: u32,
    product_constraints: Vec<String>,
    sum_constraints: Vec<String>,
}

impl CompatCircuitR1csJson {
    fn to_empty_plonk_circuit(&self) -> (PlonkCircuit<F>, HashMap<String, F>) {
        let n_vars: u32 = self.wire_count;
        let prods: Vec<(u32, u32, u32)> = self
            .product_constraints
            .iter()
            .map(|constraint| parse_constraint(constraint))
            .collect();
        let sums: Vec<(u32, u32, u32)> = self
            .sum_constraints
            .iter()
            .map(|constraint| parse_constraint(constraint))
            .collect();

        // pub_var_range: if a public wire is never used in constraints, exclude it
        // TODO: throw an error if not a single public wire is used
        let pub_var_range: Vec<u32> = (0..self.public_wire_count)
            .filter(|i| {
                // i exists in prods or sums
                prods.iter().any(|(a, b, _)| *a == *i || *b == *i)
                    || sums.iter().any(|(a, b, _)| *a == *i || *b == *i)
            })
            .collect();

        // pub_vars: for all wires whose index is less than public_wire_count, use the index as the wire name
        let pub_vars: HashMap<u32, String> = pub_var_range
            .iter()
            .map(|i| *i)
            .map(|i| (i as u32, i.to_string()))
            .collect();

        let public_values: HashMap<u32, F> = pub_var_range
            .iter()
            .map(|i| *i)
            .map(|i| {
                let t = {
                    if self.wire_values[i as usize].is_secret_share {
                        panic!("Public wire value cannot be a secret share");
                    } else {
                        F::from(self.wire_values[i as usize].value.parse::<F>().unwrap())
                    }
                };
                (i, t)
            })
            .collect();

        let named_public_values: HashMap<String, F> = pub_vars
            .iter()
            .map(|(k, v)| (v.clone(), public_values[k]))
            .collect();

        let v_c = PlonkCircuit {
            n_vars,
            pub_vars,
            prods,
            sums,
            values: None,
        };

        (v_c, named_public_values)
    }
    fn to_plonk_circuit_local(&self) -> (PlonkCircuit<F>, PlonkCircuit<F>, HashMap<String, F>) {
        let (v_c, named_public_values) = self.to_empty_plonk_circuit();
        let values: Vec<F> = self
            .wire_values
            .iter()
            .map(|wire_value| {
                if wire_value.is_secret_share {
                    panic!("Wire value cannot be a secret share");
                } else {
                    F::from(wire_value.value.parse::<F>().unwrap())
                }
            })
            .collect();

        let mut c = {
            let mut t = v_c.clone();
            t.values = Some(values);
            t
        };

        // pad to power of 2
        c.pad_to_power_of_2();
        let v_c = {
            let mut t = c.clone();
            t.values = None;
            t
        };

        (v_c, c, named_public_values)
    }
    fn to_plonk_circuit_multi_party(
        &self,
    ) -> (PlonkCircuit<F>, PlonkCircuit<MF>, HashMap<String, F>) {
        let (v_c, named_public_values) = self.to_empty_plonk_circuit();
        // values: for all wires, if the wire is a secret share, use MF::from_add_shared, else use MF::from_public
        let values: Vec<MF> = self
            .wire_values
            .iter()
            .map(|wire_value| {
                if wire_value.is_secret_share {
                    MF::from_add_shared(wire_value.value.parse().unwrap())
                } else {
                    MF::from_public(wire_value.value.parse().unwrap())
                }
            })
            .collect();

        let mut c = {
            let t = v_c.clone();
            let c = PlonkCircuit {
                n_vars: t.n_vars,
                pub_vars: t.pub_vars,
                prods: t.prods,
                sums: t.sums,
                values: Some(values),
            };
            c
        };

        // pad to power of 2
        c.pad_to_power_of_2();
        let v_c = {
            let t = c.clone();
            let v_c = PlonkCircuit {
                n_vars: t.n_vars,
                pub_vars: t.pub_vars,
                prods: t.prods,
                sums: t.sums,
                values: None,
            };
            v_c
        };

        (v_c, c, named_public_values)
    }
}

fn parse_constraint(constraint: &str) -> (u32, u32, u32) {
    let parts: Vec<&str> = constraint
        .trim_matches(|c| c == '(' || c == ')')
        .split("|")
        .collect();
    let a: u32 = parts[0].parse().unwrap();
    let b: u32 = parts[1].parse().unwrap();
    let c: u32 = parts[2].parse().unwrap();
    (a, b, c)
}

fn read_compat_circuit_r1cs_json(filename: &str) -> Result<CompatCircuitR1csJson> {
    let mut file = File::open(filename).unwrap();
    let mut contents = String::new();
    file.read_to_string(&mut contents).unwrap();
    let circuit: CompatCircuitR1csJson = serde_json::from_str(&contents).unwrap();
    Ok(circuit)
}

pub fn local_prove_and_verify_compat_circuit(filename: &str) {
    use relations::flat::*;

    // read circuit from file
    let compat_circuit = read_compat_circuit_r1cs_json(filename).unwrap();
    let (v_c, c, public) = compat_circuit.to_plonk_circuit_local();
    c.self_test();

    let v_circ = CircuitLayout::from_circuit(&v_c);

    // setup
    let setup_rng = &mut test_rng();
    let zk_rng = &mut test_rng();

    let srs = LocalPlonk::universal_setup(v_c.n_gates().next_power_of_two(), setup_rng);
    let (pk, vk) = LocalPlonk::circuit_setup(&srs, &v_circ);

    // data circuit
    let circ = CircuitLayout::from_circuit(&c);

    let pf = LocalPlonk::prove(&pk, &circ, zk_rng);
    LocalPlonk::verify(&vk, &v_circ, pf, &public);
}

pub fn mpc_prove_and_verify_compat_circuit(filename: &str) {
    use relations::flat::*;

    // read circuit from file
    let compat_circuit = read_compat_circuit_r1cs_json(filename).unwrap();
    let (v_c, c, public) = compat_circuit.to_plonk_circuit_multi_party();
    let v_circ = CircuitLayout::from_circuit(&v_c);

    // setup
    let setup_rng = &mut test_rng();
    let srs = LocalPlonk::universal_setup(v_c.n_gates().next_power_of_two(), setup_rng);
    let (pk, vk) = LocalPlonk::circuit_setup(&srs, &v_circ);

    // data circuit
    let circ = CircuitLayout::from_circuit(&c);

    let t = start_timer!(|| "timed section");
    let mpc_pk = ProverKey::from_public(pk);
    let mpc_pf = MpcPlonk::prove(&mpc_pk, &circ, &mut test_rng());
    let pf = mpc_pf.reveal();
    end_timer!(t);
    LocalPlonk::verify(&vk, &v_circ, pf, &public);
}
