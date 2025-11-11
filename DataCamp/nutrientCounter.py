import numpy as np
import torch
from PIL import Image
import matplotlib.pyplot as plt

from sam2.build_sam import build_sam2_hf
from sam2.automatic_mask_generator import SAM2AutomaticMaskGenerator

device = "cpu"  # your torch is CPU-only
sam2 = build_sam2_hf("facebook/sam2.1-hiera-large", device=device)

mask_generator = SAM2AutomaticMaskGenerator(
    sam2,
    points_per_side=24,      # lighter on CPU
    pred_iou_thresh=0.88,
    stability_score_thresh=0.95,
    crop_n_layers=1,
    min_mask_region_area=50,
    output_mode="binary_mask",
)

img = np.array(Image.open("DataCamp/coco.jpg").convert("RGB"))

with torch.inference_mode():
    anns = mask_generator.generate(img)

masks = np.stack([a["segmentation"] for a in anns], axis=0) if anns else np.zeros((0, *img.shape[:2]), bool)

overlay = img.astype(float)/255.0
rng = np.random.default_rng(0)
for m in masks:
    color = rng.random(3)
    overlay[m] = 0.2*overlay[m] + 0.8*color

plt.figure(figsize=(8,6)); plt.imshow(overlay); plt.axis("off"); plt.show()
