import torch
import torch.nn as nn
import torch.nn.functional as F

class NeuTraLAD(nn.Module):
    def __init__(self, input_dim, hidden_dim, temperature=1, K=11):
        super().__init__()
        # K learnable transformations
        self.transforms = nn.ModuleList([
            nn.Sequential(nn.Linear(input_dim, hidden_dim), nn.Tanh(),
                          nn.Linear(hidden_dim, input_dim))
            for _ in range(K)
        ])
        # Shared encoder
        self.encoder = nn.Sequential(
            nn.Linear(input_dim, hidden_dim), nn.ReLU(),
            nn.Linear(hidden_dim, hidden_dim)
        )
        self.temperature = temperature
    
    def similarity(self, z, z_k):
        return torch.exp(F.cosine_similarity(z, z_k, dim=-1)/self.temperature)

    def score(self, x):
        z = self.encoder(x)                                                      # (B, D)
        transformations = [self.encoder(T(x)) for T in self.transforms]          # K*(B, D)
        scores = []
        for k, z_k in enumerate(transformations):
            sim_z_zk = self.similarity(z, z_k)                                   # (B,)
            neg_sum = sum(
                self.similarity(z_l, z_k)
                for l, z_l in enumerate(transformations) if l != k
            )                                                                    # (B,)
            scores.append(torch.log(sim_z_zk / (sim_z_zk + neg_sum)))            # (B,)
        return -torch.stack(scores, dim=0).sum(dim=0)

    def forward(self, x):
        return self.score(x)                                                     # scalar