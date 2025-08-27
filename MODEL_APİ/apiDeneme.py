from flask import Flask, request, jsonify
import numpy as np
import tensorflow as tf
import pickle

# Flask başlat
app = Flask(__name__)

# Model ve tokenizer'ı yükle
model = tf.keras.models.load_model('c:/Users/omerf/Desktop/chatbotAPI/chatbot_bleu_25.h5')

with open('c:/Users/omerf/Desktop/chatbotAPI/tokenizer_bleu_25.pkl', 'rb') as f:
    tokenizer = pickle.load(f)

# Parametreler
max_len_q = 41  # encoder input max length
max_len_a = 154 # decoder input max length  154
start_token = '<start>'
end_token = '<end>'
vocab_size = len(tokenizer.word_index) + 1

# Çeviri fonksiyonu
def predict_answer(question):
    # Soruyu tokenize et ve pad et
    question_seq = tokenizer.texts_to_sequences([question])
    question_seq = tf.keras.preprocessing.sequence.pad_sequences(question_seq, maxlen=max_len_q, padding='post')

    # Başlangıç tokenı ile decoder input başlat
    answer_seq = [tokenizer.word_index[start_token]]
    decoded_sentence = ''

    for i in range(max_len_a):
        decoder_input = tf.keras.preprocessing.sequence.pad_sequences([answer_seq], maxlen=max_len_a, padding='post')
        
        preds = model.predict([question_seq, decoder_input], verbose=0)
        predicted_id = np.argmax(preds[0, i, :])

        # End token geldiyse dur
        if predicted_id == tokenizer.word_index.get(end_token):
            break

        predicted_word = tokenizer.index_word.get(predicted_id, '')
        decoded_sentence += ' ' + predicted_word
        answer_seq.append(predicted_id)

    return decoded_sentence.strip()

@app.route('/api/chat', methods=['POST'])
def chat():
    data = request.json
    question = data.get('question','')
    history = data.get('history',[])

    if not question:
        return jsonify({'error':'Soru boş olamaz'}), 400
    
    # sohbet hafızası için güncellenen yer
    recent_history = history[-2:] # son iki veriyi alır
    
    context_prompt= ""
    if recent_history:
        for turn in recent_history:
            user_q = turn.get('question','')
            bot_a = turn.get('answer','')
            context_prompt += f"Question: {user_q}\nAnswer: {bot_a}\n"

    context_prompt += f"Question: {question}"

    answer = predict_answer(context_prompt)

    # güncelleme bitti

    return jsonify({'Answer': answer})

if __name__ == '__main__':
    app.run(debug=True)