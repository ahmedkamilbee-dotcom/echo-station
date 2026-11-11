#!/usr/bin/env bash
# يحوّل تسجيل نظيف (من الموبايل) لصوت راديو أو لاسلكي أو شريط قديم، ويطلّعه .ogg جاهز لـ Unity.
# الاستعمال:  bash tools/oldmedia.sh radio|walkie|cassette input.m4a output.ogg
# يحتاج ffmpeg.
set -e
mode="$1"; in="$2"; out="$3"
case "$mode" in
  radio)    f="highpass=f=300,lowpass=f=3400,acompressor=threshold=-20dB:ratio=6,volume=2.2,alimiter=limit=0.8,aecho=0.6:0.3:12:0.2" ; n=0.03 ;;
  walkie)   f="highpass=f=450,lowpass=f=2600,acompressor=threshold=-24dB:ratio=10,volume=3.2,alimiter=limit=0.7" ; n=0.05 ;;
  cassette) f="highpass=f=80,lowpass=f=7000,vibrato=f=0.6:d=0.12,vibrato=f=7:d=0.04,acompressor=threshold=-18dB:ratio=3" ; n=0.012 ;;
  *) echo "mode: radio | walkie | cassette"; exit 1 ;;
esac
dur=$(ffprobe -v error -show_entries format=duration -of csv=p=0 "$in")
ffmpeg -v error -y -i "$in" -f lavfi -t "$dur" -i "anoisesrc=color=pink:amplitude=$n" \
  -filter_complex "[0:a]aformat=channel_layouts=mono,$f[v];[1:a]highpass=f=500,lowpass=f=5000[nz];[v][nz]amix=inputs=2:normalize=0[a]" \
  -map "[a]" -ac 1 -c:a libvorbis -q:a 5 "$out"
echo "✓ $out"
