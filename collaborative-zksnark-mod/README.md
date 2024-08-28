This folder contains the authors' modifications to the Collaborative zk-SNARKs by Alex Ozdemir et al.
The modified Collaborative zk-SNARKs program serves as a component of the entire CompatCircuit solution, generating proofs where the (private) inputs -- the (private) outputs of the CompatCircuit computation -- are secretly shared among all parties.

The patch file 'collaborative-zksnark.CompatCircuit.patch.xz' contains differences from commit 8cff2c2c45cbc4d5c126082a3d1521f3e662475b, available at https://github.com/alex-ozdemir/collaborative-zksnark/

To apply the patch, follow these steps:
```bash
git clone https://github.com/alex-ozdemir/collaborative-zksnark/
cd collaborative-zksnark
git checkout 8cff2c2c45cbc4d5c126082a3d1521f3e662475b
# copy 'collaborative-zksnark.CompatCircuit.patch.xz' file to this directory
xz -d -- collaborative-zksnark.collaborative-zksnark.CompatCircuit.patch.xz
git apply -- collaborative-zksnark.collaborative-zksnark.CompatCircuit.patch
```
