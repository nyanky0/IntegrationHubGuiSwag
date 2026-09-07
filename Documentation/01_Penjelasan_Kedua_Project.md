# 📦 Penjelasan Kedua Project dalam Solusi

Dokumen ini menjelaskan struktur arsitektur dan rincian kedua project yang berada di dalam satu solution **`SOLTIUS - Scheduler Add-On.sln`**.

---

## 1. Ikhtisar Solusi (Solution Overview)

Solution ini dirancang untuk kebutuhan integrasi data antara **aplikasi pihak ketiga (external application)** dan sistem ERP **SAP Business One (SAP B1)** menggunakan arsitektur **Staging Database**.

File Solution: [`SOLTIUS - Scheduler Add-On.sln`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On.sln)

```
SOLTIUS - Scheduler Add-On.sln
│
├── 📁 SOLTIUS - Web API Add-On          (.NET 8 - ASP.NET Core Web API)
│   └── Pintu masuk data dari luar ke Staging Database
│
└── 📁 SOLTIUS - Scheduler Add-On        (.NET Framework 4.8 - WinForms & Windows Service)
    └── Pemroses data otomatis dari Staging Database ke SAP Business One via DI API (COM)
```

### Tabel Perbandingan Kedua Project

| Atribut | Project A: SOLTIUS - Web API Add-On | Project B: SOLTIUS - Scheduler Add-On |
| :--- | :--- | :--- |
| **Direktori** | [`SOLTIUS - Web API Add-On`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On) | [`SOLTIUS - Scheduler Add-On`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On) |
| **Target Framework** | `.NET 8.0` (`net8.0`) | `.NET Framework 4.8` (`net48`) |
| **Tipe Aplikasi** | Web API / HTTP REST Service (Kestrel) | Dual-mode: Desktop GUI (WinForms) & Background Service (Windows Service) |
| **Bahasa** | C# 12 | C# 7.3 / C# 8 (.NET Framework) |
| **Koneksi SAP B1** | **Tidak Terhubung Langsung** ke SAP B1 | **Terhubung Langsung** via SAP DI API COM (`SAPbobsCOM.dll`) |
| **Interaksi Database** | Menulis data transaksi ke **Staging DB**; menulis log ke **Log DB** | Membaca transaksi pending dari **Staging DB**; update status; menulis log lokal |
| **Pustaka Utama** | Dapper, Microsoft.Data.SqlClient, MySql.Data, JwtBearer, Swashbuckle | `SAPbobsCOM` (COM Reference v10.0), ClosedXML, RestSharp, Newtonsoft.Json |
| **Target Pengguna** | Server backend / Sistem eksternal (Machine-to-Machine) | Administrator / Operator integrasi IT |

---

## 2. Project 1: SOLTIUS - Web API Add-On

### 2.1 Tujuan & Peran
Project ini berfungsi sebagai **Gateway API aman** yang menerima data transaksi (khususnya Sales Order) dari aplikasi luar melalui protokol HTTPS/HTTP REST. Web API tidak memproses bisnis logic SAP secara langsung, melainkan memvalidasi payload dan menyimpannya ke **Staging Database** dengan status `Pending` (`process_status = 0`).

### 2.2 Arsitektur & Struktur Direktori
Struktur direktori internal:
- [`Program.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Program.cs): Titik masuk (entry point), inisialisasi DI container, konfigurasi Kestrel, autentikasi JWT, rate limiting, channel audit log, dan middleware pipeline.
- [`Controllers/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers):
  - [`AuthController.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers/AuthController.cs): Endpoint OAuth2 Client Credentials (`/oauth2/token`) dan token refresh (`/oauth2/refresh`).
  - [`SalesOrderController.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers/SalesOrderController.cs): Menerima transaksi Sales Order (`POST /api/SalesOrder`).
  - [`ProfileSyncController.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers/ProfileSyncController.cs): Menerima XML konfigurasi profil staging database dari Scheduler (`POST /api/ProfileSync`).
  - [`StatusController.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers/Status/StatusController.cs): Endpoint pengecekan status kesiapan koneksi dan konfigurasi database (`GET /api/Status`).
- [`Services/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services):
  - [`SalesOrderService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/SalesOrderService.cs): Logika bisnis penyimpanan order dan validasi ketersediaan konfigurasi database.
  - [`AuditLogService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/AuditLog/AuditLogService.cs): Menulis log request HTTP dan sinkronisasi ke dalam `System.Threading.Channels.Channel<T>` secara non-blocking.
  - [`LogFlushWorker.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/AuditLog/LogFlushWorker.cs): `BackgroundService` yang melakukan batch insert log dari channel ke Log Database setiap 5 detik atau 100 entri.
  - [`ConfigurationService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/Configuration/ConfigurationService.cs): Mengelola pembacaan dan penulisan file XML konfigurasi runtime ([`Configuration/Config.xml`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Configuration/Config.xml)).
- [`Repositories/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Repositories):
  - [`SalesOrderRepository.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Repositories/SalesOrderRepository.cs): Menggunakan Dapper dan transaksi database untuk insert atomic ke tabel `sales_order_header` dan `sales_order_detail`.
- [`Database/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Database):
  - Mengimplementasikan pola Factory (`DatabaseConnectionFactory`, `DatabaseInitializerFactory`) yang mendukung database MySQL dan Microsoft SQL Server untuk Staging DB.

### 2.3 Mekanisme Keamanan Web API
1. **Validasi Startup Kunci Rahasia**:
   Aplikasi memeriksa environment variable `JWT_SIGNING_KEY` dan `CLIENT_SECRET`. Jika kunci masih bernilai placeholder (`BABIBABI...` atau default template) atau kurang dari 32 karakter, Kestrel server menolak start (`InvalidOperationException`).
2. **Rate Limiter**:
   Setiap IP klien dibatasi maksimal 100 request per menit (Fixed Window Rate Limiting). Jika terlampaui, API mengembalikan HTTP status code `429 Too Many Requests`.
3. **Ukuran Request Body**:
   Kestrel dibatasi menerima payload maksimal 1 MB (`MaxRequestBodySize = 1048576`).

---

## 3. Project 2: SOLTIUS - Scheduler Add-On

### 3.1 Tujuan & Peran
Project ini adalah **mesin sinkronisasi inti (Integration Engine)** yang bertugas menjembatani Staging Database dengan SAP Business One melalui SAP DI API (COM Interface). Scheduler berjalan di lingkungan lokal server atau workstation yang memiliki akses jaringan ke SAP Server dan memiliki driver SAP DI API terpasang.

### 3.2 Dual-Mode Execution
Aplikasi ini memiliki arsitektur **Dual-Mode** yang ditentukan saat startup di [`Program.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Program.cs):
1. **Mode Interaktif GUI (WinForms)**:
   - Terjadi saat `Environment.UserInteractive == true` (dijalankan langsung oleh user/double click .exe).
   - Menampilkan Form Login, Dashboard sinkronisasi, filter log riwayat, ekspor Excel, konfigurasi profil SAP, serta form administrasi Windows Service.
2. **Mode Headless (Windows Service)**:
   - Terjadi saat `Environment.UserInteractive == false` (dijalankan oleh Windows Service Control Manager / `services.msc`).
   - Berjalan di background tanpa antarmuka grafis (`SchedulerWindowsService`), mengeksekusi siklus timer sinkronisasi otomatis secara berkala.

### 3.3 Struktur Direktori & Komponen Inti
- [`Program.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Program.cs): Entry point pemilihan mode GUI / Windows Service dan inisialisasi password login.
- [`SchedulerWindowsService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/SchedulerWindowsService.cs): Kelas turunan `ServiceBase` yang mengontrol lifecycle engine saat berjalan sebagai Windows Service.
- [`Security/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Security):
  - [`Encryption.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Security/Encryption.cs): Enkripsi AES untuk password akses aplikasi.
- [`Services/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services):
  - [`SapSyncService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SapSyncService.cs): Mengelola koneksi COM ke SAP (`SAPbobsCOM.Company`), pembuatan dokumen `oOrders`, pelepasan memori COM (`Marshal.ReleaseComObject`), dan penanganan error DI API.
  - [`DatabaseService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/DatabaseService.cs): Koneksi SQL Server ke database staging. Membaca pending order (`process_status = 0`), update status (`process_status = 1` untuk sukses, `2` untuk gagal, `3` untuk exceed retry), dan mengelola tabel log lokal `TBL_SYNC_HISTORY` serta `TBL_SYNC_ERROR`.
  - [`SalesOrderSyncRunner.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SalesOrderSyncRunner.cs): Orkestrator proses sinkronisasi background batch: looping data pending, pengecekan retry limit, eksekusi pembuatan dokumen SAP, dan update status.
  - [`SyncSchedulerEngine.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SyncSchedulerEngine.cs): Timer thread pool (`System.Threading.Timer`) yang mengeksekusi sinkronisasi periodik (Interval atau Realtime polling) dan mencegah overlapping cycle via flag `_isExecuting`.
  - [`ProfileSyncService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/ProfileSyncService.cs): Mengirim konfigurasi database staging aktif ke endpoint Web API (`POST /api/ProfileSync`) dan memiliki antrean retry offline (`PendingProfileSync.xml`).
  - [`ConfigService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/ConfigService.cs): Mengelola penyimpanan XML konfigurasi profil (`AllConfigurations.xml` dan `DefaultConfig.xml`).
- [`UI/`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI):
  - [`FormLogin.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormLogin.cs): Login password aplikasi.
  - [`FormPassword.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormPassword.cs): Pengaturan dan pergantian password akses.
  - [`FormMain.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormMain.cs): Halaman dashboard utama (Trigger sync manual, Dry Run simulation, data log viewer, export Excel, retry dialog).
  - [`FormChooseCF.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormChooseCF.cs): Memilih dan mengaktifkan profil integrasi aktif.
  - [`ManageProfile.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/ManageProfile.cs) & [`FormCreate.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormCreate.cs): Membuat dan mengubah parameter koneksi SAP B1 & Staging DB.
  - [`FormSettingScheduler.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormSettingScheduler.cs): Mengatur frekuensi interval/realtime timer, serta mengelola instalasi Windows Service (`Install`, `Uninstall`, `Start`, `Stop` via `sc.exe`).

---

## 4. Hubungan dan Alur Komunikasi Antar Project

Hubungan antara kedua project dalam solusi ini bersifat **decoupled (tidak bergantung langsung)**, dengan interaksi sebagai berikut:

```
+---------------------------+                      +----------------------------+
|  SOLTIUS - Web API        |                      |  SOLTIUS - Scheduler       |
|  (.NET 8)                 |                      |  (.NET Framework 4.8)      |
+---------------------------+                      +----------------------------+
      ▲               │                                  │               ▲
      │ (1) Push      │ (2) Tulis                        │ (3) Polling   │ (4) Update
      │ Profil XML    │ Transaksi                        │ Data Pending  │ Status
      │               ▼                                  ▼               │
+───────────────────────────+                      +────────────────────────────+
|   Configuration/Config.xml|                      |       STAGING DATABASE     |
|   (Staging DB Credential) |                      |    (sales_order_header,    |
+───────────────────────────+                      |     sales_order_detail)    |
                                                   +────────────────────────────+
                                                                 │
                                                                 │ (5) Buat Dokumen
                                                                 ▼
                                                   +────────────────────────────+
                                                   |       SAP BUSINESS ONE     |
                                                   |       (DI API COM)         |
                                                   +────────────────────────────+
```

1. **Sinkronisasi Konfigurasi Database (Push Profile)**:
   Ketika operator mengubah atau memilih profil database staging di Scheduler Add-On, Scheduler memanggil endpoint `POST /api/ProfileSync` di Web API untuk mengirimkan parameter koneksi staging. Web API menyimpannya ke [`Config.xml`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Configuration/Config.xml) agar Web API tahu database staging mana yang harus dituju saat klien luar mengirim data transaksi.
2. **Pertukaran Data Transaksi Melalui Staging DB**:
   - Web API **hanya menulis** ke tabel `sales_order_header` dan `sales_order_detail` di database staging dengan `process_status = 0`.
   - Scheduler Add-On **hanya membaca langsung** dari database staging tersebut (tanpa melalui Web API) untuk mengambil data `process_status = 0`, lalu memprosesnya ke SAP B1.
   - Scheduler mengupdate kolom `process_status`, `retrycount`, `errormessage`, dan `processed_at` di tabel database staging setelah proses SAP selesai.
