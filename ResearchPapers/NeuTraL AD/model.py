import torch
import torch.nn as nn
import torch.nn.functional as F

class NeuTraLAD(nn.Module):
    def __init__(self, input_dim, hidden_dim, depth, temperature=1, K=11):
        super().__init__()
        # K learnable transformations
        self.transforms = nn.ModuleList([self.build_mlp(input_dim, hidden_dim, input_dim, depth)
            for _ in range(K)
        ])
        # Shared encoder
        self.encoder = self.build_mlp(input_dim, hidden_dim, hidden_dim, depth)
        self.temperature = temperature
        self.K = K

    def build_mlp(self, input_dim, hidden_dim, output_dim, depth=2, drop_out=0.3):
        assert depth >= 2
        layers = []
        layers.append(nn.Linear(input_dim, hidden_dim))
        layers.append(nn.GELU())

        for _ in range(depth-2):
            layers.append(nn.Dropout(p=drop_out))
            layers.append(nn.Linear(hidden_dim, hidden_dim))
            layers.append(nn.GELU())

        layers.append(nn.Dropout(p=drop_out))
        layers.append(nn.Linear(hidden_dim, output_dim))
        
        return nn.Sequential(*layers)
    
    def similarity(self, z, z_k):
        return torch.exp(F.cosine_similarity(z, z_k, dim=-1)/self.temperature)

    def forward(self, x):
        z = self.encoder(x)                                                      # (B, D)
        transformations = [self.encoder(T(x)) for T in self.transforms]          # K*(B, D)
        scores = []
        for k, z_k in enumerate(transformations):
            sim_z_zk = self.similarity(z, z_k)                                   # (B,)
            neg_sum = sum(
                self.similarity(z_l, z_k)
                for l, z_l in enumerate(transformations) if l != k
            )                                                                    # (B,)
            scores.append(torch.log(sim_z_zk / (sim_z_zk + neg_sum)))            # K*(B,)
        return -torch.stack(scores, dim=0).sum(dim=0)                            # (B,) 