#!/bin/sh
# Whip strike for WHIP (by Lekha, AnimateDiff) -> Assets/Resources/Art/Whip/lash.png
# 15 frames, 96x79 each: mirrored to face right, the off-white background keyed out, 58 px tall like the other girls.
# Run from Game/:  sh Tools/make_whip_frames.sh   (needs ffmpeg)
ffmpeg -v error -y -i Tools/src/whip_lash.mp4 \
  -vf "crop=850:706:260:30,hflip,colorkey=0xFAFAFA:0.10:0,scale=96:79:flags=area,tile=15x1" \
  -frames:v 1 -pix_fmt rgba Assets/Resources/Art/Whip/lash.png
