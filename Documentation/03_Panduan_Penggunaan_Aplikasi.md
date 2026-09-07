# 📖 Panduan Penggunaan Aplikasi (Berdasarkan Kode Saat Ini)

Panduan ini berisi petunjuk langkah demi langkah mengenai cara mengonfigurasi, menjalankan, dan mengoperasikan kedua project (**SOLTIUS - Web API Add-On** dan **SOLTIUS - Scheduler Add-On**) sesuai dengan kode program saat ini.

---

## 1. Prasyarat Sistem (System Requirements)

Sebelum menjalankan aplikasi, pastikan komputer/server memenuhi persyaratan berikut:

| Komponen | Spesifikasi / Kebutuhan |
| :--- | :--- |
| **Sistem Operasi** | Windows 10, Windows 11, atau Windows Server (2016 / 2019 / 2022) |
| **Framework Web API** | [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) atau .NET 8.0 ASP.NET Core Runtime |
| **Framework Scheduler** | [.NET Framework 4.8 Developer Pack / Runtime](https://dotnet.microsoft.com/download/dotnet-framework/net48) |
| **Database Server** | Microsoft SQL Server (2016 atau lebih baru) untuk database Staging dan Log |
| **SAP Driver** | **SAP Business One Client DI API** terinstal di mesin lokal (versi 10.0 atau sesuai referensi COM `SAPbobsCOM.dll`) |
| **Hak Akses** | Administrator lokal (dibutuhkan saat instalasi Windows Service dan pendaftaran COM DI API) |

---

## 2. Menyiapkan Database

Buat 2 buah database kosong di Microsoft SQL Server:
1. **`test`** — Sebagai Database Staging (penyimpanan sementara transaksi dari API luar).
2. **`log`** — Sebagai Database Audit Log Web API.

> [!NOTE]
> Tabel-tabel transaksi (`sales_order_header`, `sales_order_detail`, `api_logs`, `sync_logs`, `TBL_SYNC_HISTORY`, `TBL_SYNC_ERROR`) akan **dibuat secara otomatis** oleh aplikasi saat pertama kali terhubung ke database.

---

## 3. Panduan Menjalankan & Menggunakan Web API Add-On

### 3.1 Mengatur Environment Variables
Web API menerapkan proteksi startup yang mewajibkan pengaturan secret key. Buka **Command Prompt (CMD)** atau **PowerShell** dan jalankan perintah berikut:

```cmd
:: Konfigurasi Rahasia JWT & Klien
set JWT_SIGNING_KEY=KunciRahasiaKarakterAcakMinimal32KarakterPanjangnya!
set CLIENT_SECRET=SecretUntukClientSchedulerAddon2026!

:: Konfigurasi Staging Database (Default SQL Server)
set STAGING_DB_SERVER=localhost
set STAGING_DB_NAME=test
set STAGING_DB_USER=sa
set STAGING_DB_PASS=PasswordDB123!

:: Konfigurasi Log Database
set LOG_DB_SERVER=localhost
set LOG_DB_NAME=log
set LOG_DB_USER=sa
set LOG_DB_PASS=PasswordDB123!
```

> [!WARNING]
> Web API **TIDAK AKAN START** jika `JWT_SIGNING_KEY` bernilai default/placeholder atau kurang dari 32 karakter.

### 3.2 Menjalankan Server Web API
Masuk ke folder project Web API dan jalankan:

```cmd
cd "D:\Metrodata\IBT\Template_Addon_Staging_2026_Auth 2\Template_Addon_Staging_2026_Auth\SOLTIUS - Web API Add-On"
dotnet run --urls http://localhost:5006
```

Atau buka project di Visual Studio dan tekan **F5** / **Ctrl+F5**.

### 3.3 Mengakses Dokumentasi API (Swagger)
Ketika dijalankan pada environment `Development`, buka peramban web di:
```
http://localhost:5006/swagger
```
Dokumentasi interaktif OpenAPI akan menampilkan seluruh endpoint yang tersedia.

### 3.4 Langkah Integrasi dari Klien Luar (Third Party)

#### Langkah 1: Meminta Access Token OAuth2
Kirim request HTTP POST ke endpoint `/oauth2/token`:

```bash
curl -X POST "http://localhost:5006/oauth2/token" \
     -H "Content-Type: application/json" \
     -d '{
       "grant_type": "client_credentials",
       "client_id": "scheduler-addon",
       "client_secret": "SecretUntukClientSchedulerAddon2026!"
     }'
```

**Contoh Response Berhasil:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

#### Langkah 2: Mengirim Sales Order ke Staging
Kirim request HTTP POST ke endpoint `/api/SalesOrder` dengan menyertakan header `Authorization: Bearer <access_token>`:

```bash
curl -X POST "http://localhost:5006/api/SalesOrder" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <access_token_disini>" \
     -d '{
       "cardCode": "C20000",
       "cardName": "PT Pelanggan Sejahtera",
       "docDate": "2026-09-07T00:00:00",
       "docDueDate": "2026-09-14T00:00:00",
       "taxDate": "2026-09-07T00:00:00",
       "remarks": "Order Integrasi dari API Eksternal",
       "documentLines": [
         {
           "itemCode": "A00001",
           "itemDescription": "Produk Item A",
           "warehouseCode": "01",
           "quantity": 10.0,
           "price": 150000.0
         },
         {
           "itemCode": "A00002",
           "itemDescription": "Produk Item B",
           "warehouseCode": "01",
           "quantity": 5.0,
           "price": 275000.0
         }
       ]
     }'
```

**Response Berhasil:**
```json
{
  "success": true,
  "message": "Sales Order Received"
}
```
*Data kini tersimpan di database staging dengan `process_status = 0` (Pending).*

#### Langkah 3: Pengecekan Status Web API
Klien atau sistem monitoring dapat memeriksa kesiapan API melalui:
- `GET http://localhost:5006/health` (Tanpa Auth) -> Response status 200 OK.
- `GET http://localhost:5006/api/Status` (Dengan Header Bearer Token) -> Memeriksa kesiapan konfigurasi dan koneksi database staging.

---

## 4. Panduan Menjalankan & Menggunakan Scheduler Add-On

### 4.1 Membuka Aplikasi & Setup Password
1. Buka solusi di Visual Studio dan jalankan project **`SOLTIUS - Scheduler Add-On`** (atau jalankan file executable di `bin\Debug\SOLTIUS - Scheduler Add-On.exe`).
2. **Saat Pertama Kali Dijalankan**:
   - Sistem mendeteksi ketiadaan file keamanan, lalu memunculkan form **Setup Master Password** (`FormPassword`).
   - Masukkan password administrator baru (misal: `admin123`) dan klik **Simpan**.
3. **Login Aplikasi** (`FormLogin`):
   - Masukkan master password yang telah dibuat dan klik **Login**.
   - Jendela utama **SOLTIUS Scheduler** akan terbuka.

---

### 4.2 Mengonfigurasi Profil Koneksi (SAP B1 & Staging DB)
Agar Scheduler dapat membaca data staging dan terhubung ke SAP B1:
1. Pada Menu Bar atas, klik **Configuration** (atau klik tulisan *Active Profile* di sudut kanan bawah).
2. Jendela **Choose Configuration** (`FormChooseCF`) akan muncul:
   - Untuk membuat baru: Klik tombol **Create New Profile**.
   - Untuk mengubah: Pilih profil yang ada di daftar, lalu klik **Manage Profile**.
3. Pada Form **Manage Profile**:
   - **Profile Name**: Masukkan nama profil (contoh: `SAP_LIVE_MSSQL` atau `SAP_DEV`).
   - **Environment**: Pilih `Development`, `Testing`, atau `Production`.
   - **SAP Database Group**:
     - *Server Type*: Pilih tipe database SAP (misal: `MSSQL2019`, `MSSQL2017`, atau `HANA`).
     - *DB Server*: IP atau hostname SQL Server SAP B1 (contoh: `192.168.1.100` atau `localhost`).
     - *DB Port*: Port database (default SQL Server: `1433`).
     - *DB User & Password*: Kredensial database instance (contoh: `sa` dan passwordnya).
   - **SAP B1 Group**:
     - *SAP Database*: Nama company database di SAP B1 (contoh: `SBODEMOID`).
     - *SAP User & Password*: User login SAP Business One (contoh: `manager`).
     - *License Server*: Alamat SAP License Manager (contoh: `192.168.1.100:40000` atau `localhost:30000`).
   - **Database Staging Group**:
     - *DB Type*: Pilih **SQLServer** (Scheduler membaca staging via SQL Server).
     - *DB Server & Port*: Server staging database (contoh: `localhost`, port `1433`).
     - *DB Name*: Nama database staging (contoh: `test`).
     - *DB User & Pass*: Kredensial database staging.
     - *Web API URL*: Alamat server Web API (contoh: `http://localhost:5006`).
4. Klik **Save / Update**.
5. Pilih profil tersebut dan klik **Select Profile / OK**.
6. Sistem akan otomatis memperbarui profil aktif dan **mengirimkan parameter database staging ke Web API** (`POST /api/ProfileSync`) secara otomatis di latar belakang.

---

### 4.3 Menjalankan Sinkronisasi Manual (FormMain Dashboard)

#### Melakukan Sinkronisasi Dokumen:
1. Pada tab **Sync**:
2. Pastikan checkbox **Sales Order** dalam keadaan tercentang.
3. *(Opsional)* Jika ingin melakukan pengujian tanpa memasukkan data nyata ke SAP B1, centang **Dry Run (Mode Simulasi)**.
4. Klik tombol **Sync**:
   - Scheduler menguji koneksi ke SAP B1 via DI API.
   - Mengambil semua transaksi pending (`process_status = 0`) dari database staging.
   - Membuat dokumen Sales Order di SAP B1.
   - Memperbarui status data di database staging.
5. Kotak pesan (Message Box) akan menampilkan ringkasan dokumen yang berhasil dan gagal diproses.

#### Memantau & Memfilter Log Sinkronisasi:
1. Klik tab **Log Data**.
2. Anda dapat memfilter tampilan riwayat log:
   - **Filter Tanggal**: Ubah tanggal *From* (`dtpStgFrom`) dan *To* (`dtpStgTo`).
   - **Filter Log Level**: Pilih `All`, `Success`, atau `Failed`.
   - **Filter Fungsi**: Pilih `All` atau `Sales Order`.
   - **Pencarian Cepat**: Ketik nomor CardCode, ItemCode, DocEntry, atau kata kunci error pada kotak pencarian teks.
3. Tabel log akan otomatis ter-refresh menampilkan baris yang sesuai.

#### Melakukan Retry Transaksi yang Gagal:
1. Jika terdapat transaksi yang berstatus gagal, klik tombol **Retry Failed**.
2. Jendela dialog **Pilih Data** akan muncul menampilkan daftar seluruh transaksi yang gagal beserta pesan error dari SAP.
3. Berikan tanda centang pada transaksi yang ingin dicoba kembali.
4. Klik tombol **Retry**. Sistem akan memproses ulang transaksi terpilih ke SAP B1 dan mencatat hasilnya.

#### Ekspor Log ke Excel:
1. Pada tab **Log Data**, klik tombol **Export to Excel**.
2. Pilih lokasi penyimpanan file `.xlsx`.
3. Seluruh baris log yang sedang tampil pada tabel akan diekspor rapi lengkap dengan header kolom.

#### Backup & Restore Profil Konfigurasi:
- **Export Profil**: Menu `Configuration` -> `Export Profiles` -> Simpan file `ConfigBackup_YYYYMMDD.xml`.
- **Import Profil**: Menu `Configuration` -> `Import Profiles` -> Pilih file XML backup untuk memulihkan konfigurasi.

---

### 4.4 Mengonfigurasi & Menjalankan Mode Otomatis (Scheduler / Windows Service)

Untuk menjalankan sinkronisasi secara terjadwal otomatis tanpa intervensi manual:
1. Pada Menu Bar atas, klik **Scheduler Setting** (`FormSettingScheduler`).
2. **Pilih Mode Waktu**:
   - **Mode Interval**: Pilih opsi ini jika sinkronisasi ingin dijalankan berkala setiap **X menit** (misalnya setiap 5 menit).
   - **Mode Real-time**: Pilih opsi ini jika sistem harus melakukan polling terus-menerus setiap **X detik** (misalnya setiap 10 detik).
3. **Pilih Fungsi**:
   - Pastikan **Sync Sales Order (ORDR)** tercentang.
4. **Uji Coba Langsung (Run Now)**:
   - Anda dapat menekan tombol **Run Now** untuk menguji apakah engine background dapat mengeksekusi sinkronisasi dengan konfigurasi waktu yang dipilih.
5. Klik **Simpan** untuk menyimpan setelan ke file `SchedulerSettings.xml`.

#### Mengelola Windows Service:
Form Scheduler Setting menyediakan tombol kontrol langsung untuk Windows Service Control Manager:
1. **Nama Service**: Masukkan nama service yang diinginkan (default: `SOLTIUSSchedulerService`).
2. **Install Service**:
   - Klik tombol **Install Service**.
   - Muncul dialog konfirmasi UAC Windows (Administrator), klik **Yes**.
   - Sistem mengeksekusi `sc.exe create` secara otomatis dan mendaftarkan exe Scheduler sebagai Windows Service tipe Auto-start.
   - Status service akan berubah menjadi `Stopped`.
3. **Start Service**:
   - Klik tombol **Start Service**.
   - Windows Service akan berjalan di latar belakang (Status: `Running`). Scheduler kini bekerja otomatis secara headless bahkan saat aplikasi GUI ditutup atau server di-restart.
4. **Stop Service**:
   - Klik tombol **Stop Service** jika ingin menghentikan sementara proses otomatisasi.
5. **Uninstall Service**:
   - Hentikan service terlebih dahulu, lalu klik tombol **Uninstall Service** untuk menghapus service dari sistem Windows.
