import torch
import torch.nn as nn
import torch.nn.functional as F

class NeuTraLAD(nn.Module):
    def __init__(self, input_dim, hidden_dim, depth, temperature=1, K=11):
        super().__init__()
        # K learnable transformations
        self.transforms = nn.ModuleList(
            [self.build_mlp(input_dim, hidden_dim, input_dim, depth, use_norm_output=False)
             for _ in range(K)]
        )
        # Shared encoder: Dropout needs to be False as otherwise will get different dropout masks 
        # foreach different transformation, introducing noise and decreasing efficient learning.
        self.encoder = self.build_mlp(input_dim, hidden_dim, hidden_dim, depth, use_drop_out=False)
        self.temperature = temperature
        self.K = K

    def build_mlp(self, input_dim, hidden_dim, output_dim, depth=2, 
                  use_norm_output=True, use_drop_out=True, drop_out=0.3):
        assert depth >= 2
        layers = []
        layers.append(nn.Linear(input_dim, hidden_dim))
        layers.append(nn.GELU())

        for _ in range(depth-2):
            if use_drop_out: layers.append(nn.Dropout(p=drop_out))
            layers.append(nn.Linear(hidden_dim, hidden_dim))
            layers.append(nn.GELU())

        if use_drop_out: layers.append(nn.Dropout(p=drop_out))
        layers.append(nn.Linear(hidden_dim, output_dim))
        if use_norm_output: layers.append(nn.LayerNorm(output_dim))
        return nn.Sequential(*layers)

    def forward(self, x):
        z = self.encoder(x)                                                           # (B, D)
        transformations = [self.encoder(T(x)) for T in self.transforms]               # K*(B, D)
        scores = []
        for k, z_k in enumerate(transformations):
            log_sim_z_zk = F.cosine_similarity(z, z_k, dim=-1)/self.temperature

            log_neg_terms = torch.stack([
                F.cosine_similarity(z_l, z_k, dim=-1)/self.temperature
                for l, z_l in enumerate(transformations) if l != k
            ], dim=0)

            all_terms = torch.cat([log_sim_z_zk.unsqueeze(0), log_neg_terms], dim=0)  # (K, B)
            log_denominator = torch.logsumexp(all_terms, dim=0)                       # (B,)
            scores.append(log_sim_z_zk - log_denominator)                             # K*(B,)

        return -torch.stack(scores, dim=0).sum(dim=0)