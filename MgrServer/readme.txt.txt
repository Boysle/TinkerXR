Çalıştırmak için:
docker run -it -v C:\Users\serda\Desktop\output\:/app/output -p 3000:3000 slicer-server

Değişiklik yapınca build etmek için:
docker build . -t slicer-server

Stl bunny bastırmak yerine oluşturulan objeyi bastırmak için server.ts dosyasının içinden:

cmd = cmd.replace(`%%stl_path%%`, `output/Stanford_Bunny.stl`);

yerine şunu koyun:

cmd = cmd.replace(`%%stl_path%%`, `output/model.stl`);