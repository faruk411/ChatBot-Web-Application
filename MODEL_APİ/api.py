from flask import Flask, request, jsonify
from transformers import BlipProcessor, BlipForConditionalGeneration
from PIL import Image
import torch
import io

app = Flask(__name__)

# BLIP modelini yükle
model_dir = "./blip_finetuned"
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model = BlipForConditionalGeneration.from_pretrained(model_dir).to(device)
processor = BlipProcessor.from_pretrained(model_dir)
model.eval()

@app.route("/api/process-image", methods=["POST"])
def process_image():
    if 'image' not in request.files:
        return jsonify({"error": "Görsel dosyası bulunamadı"}), 400

    file = request.files["image"]
    try:
        image = Image.open(file.stream).convert("RGB")
    except Exception as e:
        return jsonify({"error": f"Görsel işlenemedi: {str(e)}"}), 400

    # Caption (metin) üretimi
    inputs = processor(images=image, return_tensors="pt").to(device)
    with torch.no_grad():
        output_ids = model.generate(pixel_values=inputs["pixel_values"], max_length=64)
    caption = processor.decode(output_ids[0], skip_special_tokens=True)

    # Yanıtı JSON formatında döndür
    return jsonify({"answer": caption})

if __name__ == "__main__":
    app.run(debug=True, port=5001)