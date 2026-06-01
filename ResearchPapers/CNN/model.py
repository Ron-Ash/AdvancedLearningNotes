import warnings
import torch
import torch.nn as nn

class CNN(nn.Module):
    def __init__(self, 
                 input_shape: tuple[int, int, int], 
                 conv_channels: list[int], 
                 fc_dims: list[int], 
                 output_dim: int, 
                 kernel_sizes: list[int] = [3], 
                 strides: list[int] = [1], 
                 paddings: list[int] = [1], 
                 batch_norm_conv: type[nn.Module] | None = nn.BatchNorm2d, 
                 batch_norm_fc: type[nn.Module] | None = nn.BatchNorm1d, 
                 pooling: type[nn.Module] | None = nn.AvgPool2d, 
                 activation: type[nn.Module] | None = nn.GELU, 
                 dropout: float = 0.0):
        super().__init__()
        in_channels, in_height, in_width = input_shape
        self.conv, flat_dim = self.build_conv_block(in_channels, (in_height, in_width), conv_channels, kernel_sizes, 
                                                    strides, paddings, batch_norm=batch_norm_conv, pooling=pooling,
                                                    activation=activation, dropout=dropout)
        self.fc = self.build_fc_mlp(flat_dim, fc_dims, output_dim, batch_norm=batch_norm_fc, activation=activation,
                                     dropout=dropout)
    

    def build_conv_block(self, 
                         in_channels: int, 
                         input_shape: tuple[int, int], 
                         conv_channels: list[int], 
                         kernel_sizes: list[int] = [3], 
                         strides: list[int] = [1], 
                         paddings: list[int] = [1], 
                         min_spatial_size: int = 4, 
                         batch_norm: type[nn.Module] | None = None, 
                         activation: type[nn.Module] = nn.GELU, 
                         pooling: type[nn.Module] | None = None,
                         dropout: float = 0.0 
                         ) -> tuple[nn.Sequential, int]:
        H, W = input_shape
        layers = []
        inter_channel = in_channels
        for index, inter_channel in enumerate(conv_channels):
            kernel_size = kernel_sizes[min(index, len(kernel_sizes)-1)]
            padding = paddings[min(index, len(paddings)-1)]
            stride = strides[min(index, len(strides)-1)]
            # convolution shape alteration (as per pytorch Conv2d documentation) 
            newH = (H+2*padding-kernel_size)//stride + 1
            newW = (W+2*padding-kernel_size)//stride + 1
            if min(H,W)<min_spatial_size or min(newH,newW)<min_spatial_size:
                warnings.warn(f"Early stopping at layer {index}: spatial dims would drop below {min_spatial_size}")
                break
            H,W = newH, newW
            conv2d = nn.Conv2d(in_channels, inter_channel, kernel_size=kernel_size, padding=padding, stride=stride)
            layers.append(conv2d)

            if batch_norm: layers.append(batch_norm(inter_channel))
            layers.append(activation())
            if pooling and (min(H,W)//2 >= min_spatial_size): 
                layers.append(pooling(kernel_size=2))
                H, W = H // 2, W // 2
            if dropout > 0.0: layers.append(nn.Dropout2d(p=dropout)) # drops entire feature maps
            in_channels = inter_channel
        flat_dim = inter_channel * H * W
        return nn.Sequential(*layers), flat_dim
    

    def build_fc_mlp(self, 
                     input_dim: int, 
                     fc_dims: list[int], 
                     output_dim: int, 
                     batch_norm: type[nn.Module] | None = None,
                     activation: type[nn.Module] = nn.GELU, 
                     dropout: float = 0.0
                     ) -> nn.Sequential:
        layers = []
        for inter_dim in fc_dims:
            layers.append(nn.Linear(input_dim, inter_dim))
            if batch_norm: layers.append(batch_norm(inter_dim))
            layers.append(activation())
            if dropout > 0.0: layers.append(nn.Dropout(p=dropout))
            input_dim = inter_dim

        layers.append(nn.Linear(input_dim, output_dim))
        return nn.Sequential(*layers)
    

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        assert x.ndim == 4, f"Expected 4D input (B, C, H, W), got {x.ndim}D"
        x = self.conv(x)
        x = x.flatten(1)
        x = self.fc(x)
        return x
        