# ChatBot Web Uygulaması

### SinAi ( Tıbbi Asistan )
Bu web uygulamasında kendi eğittim iki adet model bulunuyor, ilki tıbbi sorulara cevap veren model. Bu model daha çok doktorların sorularına cevap veren bir nevi doktorlar yardımcı olan asistan.
Bu tıbbi asistan modeli, 44 bin veri ile eğitilip 64 milyon parametreye sahiptir. Bleu skoruda %26 olarak ölçülmüştür, bu değer başlangıç için kabul edilebilen bir değerdir, ileriki zamanlarda bu değer arttırılması hedeflenmektedir.

### VisionAi ( Görselden Anlam Çıkaran Model)

İkinci modelimiz ise modele yüklenen görselin ne olduğunu anlatan model, 20 bin veri ile transformer mimarisi olan Blip model Fine-Tune edilmiştir. Bu modelinde Bleu skoru %27 olarak ölçüldü.
İlerleyen süreçlerde bu model tıbbi görüntü analizi için tekrar eğitilecektir.


### Uygulama Görselleri
<img width="1675" height="747" alt="Ekran görüntüsü 2025-08-24 024847" src="https://github.com/user-attachments/assets/ac707ed1-0e02-43d4-8084-ee8cb49f82b2" />

<img width="1695" height="816" alt="Ekran görüntüsü 2025-08-24 024933" src="https://github.com/user-attachments/assets/bd1cd2fa-98da-4cf1-a528-fab03713aca2" />
