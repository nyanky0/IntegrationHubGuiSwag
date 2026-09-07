# ⚙️ Cara Kerja Aplikasi (Berdasarkan Kode Saat Ini)

Dokumen ini menjelaskan alur teknis dan mekanisme operasional sistem berdasarkan kondisi kode sumber saat ini.

---

## 1. Alur Kerja Menyeluruh (End-to-End Workflow)

Sistem bekerja dalam siklus integrasi 5 tahap:

```
[ Klien Luar / Third Party ]
            │
    (1) POST /oauth2/token  (Client ID & Secret)
            ▼
[ SOLTIUS - Web API Add-On ] ── Menghasilkan JWT Access Token (Masa aktif 60 menit)
            │
    (2) POST /api/SalesOrder (Header Bearer JWT + Body JSON)
            ▼
[ SOLTIUS - Web API Add-On ] ── Validasi DTO & Kuota Klien
            │
            ├── Simpan Audit Log ke Channel Buffer ──▶ [ Log DB: api_logs ]
            │
            └── Insert Transaksi (Status = 0: Pending)
                        ▼
            ┌─────────────────────────────┐
            │     STAGING DATABASE        │
            │  - sales_order_header       │
            │  - sales_order_detail       │
            └──────────────┬──────────────┘
                           │
    (3) Polling berkala query WHERE process_status = 0
                           ▼
            ┌─────────────────────────────┐
            │ SOLTIUS - Scheduler Add-On  │
            │ (Engine / Manual / Service) │
            └──────────────┬──────────────┘
                           │
    (4) Buat Dokumen via SAP DI API COM (SAPbobsCOM)
                           ▼
            ┌─────────────────────────────┐
            │   SAP BUSINESS ONE (ERP)    │
            │   Tabel ORDR & RDR1 dibuat  │
            └──────────────┬──────────────┘
                           │
    (5) Update Status Staging (1: Sukses, 2: Gagal, 3: Exceeded Retry)
        + Tulis History ke TBL_SYNC_HISTORY & TBL_SYNC_ERROR
```

---

## 2. Cara Kerja Komponen Web API

### 2.1 Startup & Inisialisasi Database Otomatis
Ketika Web API dijalankan:
1. **Validasi Kunci Keamanan**:
   Sistem membaca konfigurasi `Jwt:SigningKey` dan `Jwt:Clients:ClientSecret`. Jika masih bernilai default/placeholder atau kurang dari 32 karakter, startup digagalkan secara eksplisit.
2. **Inisialisasi Tabel Staging & Log**:
   - Jika konfigurasi staging database ([`Configuration/Config.xml`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Configuration/Config.xml)) telah tersedia, sistem mengeksekusi script DDL otomatis (`CREATE TABLE IF NOT EXISTS`) untuk tabel `sales_order_header` dan `sales_order_detail`.
   - Jika konfigurasi `LogDatabase` di [`appsettings.json`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/appsettings.json) terisi, sistem membuat tabel `api_logs` dan `sync_logs`.
3. **Pendaftaran Background Worker**:
   Sistem mengaktifkan [`LogFlushWorker`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/AuditLog/LogFlushWorker.cs) untuk membaca antrean logging secara asynchronous.

### 2.2 Autentikasi OAuth2 & Refresh Token
1. **Mendapatkan Token** (`POST /oauth2/token`):
   - Klien mengirim payload `grant_type=client_credentials`, `client_id`, dan `client_secret`.
   - Sistem mencocokkan dengan daftar klien yang terdaftar di `appsettings.json`.
   - Jika valid, sistem menerbitkan `access_token` JWT (berlaku 60 menit) dan string `refresh_token` acak (berlaku 7 hari).
   - Refresh token disimpan di [`RefreshTokenStore`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Authentication/RefreshTokenStore.cs) (saat ini berbasis in-memory dictionary).
2. **Memperbarui Token** (`POST /oauth2/refresh`):
   - Klien menukarkan `refresh_token` yang valid.
   - Token lama langsung dihapus dari memory (*single-use rotation*) dan token baru diterbitkan.

### 2.3 Penerimaan Sales Order (`POST /api/SalesOrder`)
1. **Validasi Request**:
   - Controller memeriksa kelengkapan: `CardCode` (max 30 karakter), tanggal transaksi (`DocDate`, `DocDueDate`), baris detail minimal 1 dan maksimal 100 baris, `ItemCode` valid, serta kuantitas > 0.
2. **Penyimpanan ke Database (Dapper Transaction)**:
   - Dijalankan secara atomik dalam satu transaksi database:
     - Insert ke `sales_order_header` dengan default `process_status = 0`, `retrycount = 0`.
     - Mengambil `header_id` yang baru dibuat.
     - Insert seluruh item detail ke `sales_order_detail` yang berelasi dengan `header_id`.
3. **Audit Logging Non-Blocking (Channel Pattern)**:
   - Request HTTP dan detail transaksi dicatat melalui [`AuditLogService`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/AuditLog/AuditLogService.cs) ke dalam bounded `System.Threading.Channels.Channel<T>` (kapasitas 10.000, drop oldest jika penuh).
   - Background service [`LogFlushWorker`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/AuditLog/LogFlushWorker.cs) melakukan batch insert ke database log setiap 5 detik atau ketika buffer mencapai 100 entri.

### 2.4 Sinkronisasi Profil dari Scheduler (`POST /api/ProfileSync`)
- Endpoint ini menerima XML konfigurasi database dari Scheduler Add-On.
- Payload XML berisi tipe database staging (`SQLServer` atau `MySQL`), host server, port, nama database, username, dan password.
- Web API memperbarui file lokal [`Configuration/Config.xml`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Configuration/Config.xml) dan melakukan verifikasi koneksi ke database yang dituju.

---

## 3. Cara Kerja Komponen Scheduler Add-On

### 3.1 Deteksi Mode Startup
Ketika aplikasi dijalankan ([`Program.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Program.cs)):
- **User Mode (`Environment.UserInteractive == true`)**:
  1. Cek keberadaan file [`Security/AccessConfig.xml`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Security). Jika belum ada, tampilkan `FormPassword` untuk membuat master password baru.
  2. Tampilkan `FormLogin`. Pengguna memasukkan master password.
  3. Setelah login valid, jendela utama `FormMain` ditampilkan.
- **Service Mode (`Environment.UserInteractive == false`)**:
  - Windows Service Control Manager mengeksekusi `ServiceBase.Run(new SchedulerWindowsService())`.
  - Service membaca file konfigurasi `SchedulerSettings.xml` dan menyalakan engine sinkronisasi otomatis.

### 3.2 Siklus Engine Sinkronisasi Otomatis ([`SyncSchedulerEngine.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SyncSchedulerEngine.cs))
Engine berjalan menggunakan `System.Threading.Timer`:
1. **Pencegahan Overlap (`_isExecuting`)**:
   Jika siklus sebelumnya belum selesai memproses data dan timer berikutnya terpicu, siklus baru akan dilewati (*skipped*) untuk mencegah perebutan koneksi DI API dan race condition database.
2. **Pengecekan Profil Aktif**:
   Engine mengambil profil aktif dari [`ConfigService.GetActiveConfiguration()`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/ConfigService.cs). Jika profil belum diset, siklus dilewati.
3. **Pengecekan Flag Fitur**:
   - Jika `SyncSalesOrder == true`, engine memanggil runner [`SalesOrderSyncRunner.RunPendingSync`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SalesOrderSyncRunner.cs).
   - Flag `SyncServiceLayer` belum diimplementasikan di level engine.

### 3.3 Alur Eksekusi Sinkronisasi Sales Order ([`SalesOrderSyncRunner.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SalesOrderSyncRunner.cs))
1. **Membaca Data Pending**:
   Scheduler membaca data dari database staging via query:
   ```sql
   SELECT * FROM sales_order_header WHERE process_status = 0
   ```
   Beserta baris detail pasangannya dari `sales_order_detail`.
2. **Evaluasi Batas Retry**:
   Untuk setiap order, sistem memeriksa apakah `retrycount >= 5` (`IsRetryLimitExceeded`).
   - Jika melebihi batas: status diubah menjadi `process_status = 3` (Exceeded Retry Limit / Dead Letter), dan order dilewati.
3. **Koneksi ke SAP Business One via DI API**:
   Sistem membuat koneksi COM ke SAP via [`SapSyncService.ConnectToDIAPI`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SapSyncService.cs):
   - Menentukan `DbServerType` (MSSQL2016 / 2017 / 2019 / 2022 atau HANA).
   - Mengisi `Server`, `CompanyDB`, `UserName`, `Password`, dan `LicenseServer`.
   - Memanggil fungsi COM `company.Connect()`.
4. **Pembuatan Dokumen Transaksi**:
   - Mengambil objek bisnis:
     ```csharp
     SAPbobsCOM.Documents oOrder = company.GetBusinessObject(BoObjectTypes.oOrders);
     ```
   - Mengisi data header: `CardCode`, `DocDate`, `DocDueDate`, `TaxDate`, `Comments`.
   - Mengisi item baris: `ItemCode`, `Quantity`, `Price`, `WarehouseCode`, lalu memanggil `Lines.Add()`.
   - Menjalankan `oOrder.Add()`.
5. **Penanganan Hasil Eksekusi**:
   - **Jika Sukses (`returnCode == 0`)**:
     - Mengambil nomor DocEntry SAP via `company.GetNewObjectKey()`.
     - Update staging: `process_status = 1`, `retrycount = 0`, `processed_at = NOW()`.
     - Simpan catatan ke tabel log staging `TBL_SYNC_HISTORY`.
   - **Jika Gagal (`returnCode != 0` / Exception)**:
     - Mengambil pesan error dari `company.GetLastError(out errCode, out errMsg)`.
     - Update staging: `process_status = 2`, `retrycount = retrycount + 1`, `errormessage = errMsg`.
     - Simpan catatan ke `TBL_SYNC_HISTORY` dan `TBL_SYNC_ERROR`.
6. **Manajemen Memori COM**:
   Dalam blok `finally`, objek COM dilepas secara berurutan menggunakan `System.Runtime.InteropServices.Marshal.ReleaseComObject()` untuk mencegah kebocoran memori (memory leak) di runtime 32-bit/64-bit.

---

## 4. Status Tabel Staging (`process_status`)

Nilai status pada kolom `process_status` tabel `sales_order_header` dan `sales_order_detail`:

| Nilai | Deskripsi Status | Keterangan |
| :---: | :--- | :--- |
| `0` | **Pending** | Data baru masuk dari Web API dan siap diambil oleh Scheduler. |
| `1` | **Success** | Dokumen berhasil dibuat di SAP B1 (DocEntry tersimpan). |
| `2` | **Failed** | Gagal diproses ke SAP B1. Menunggu siklus retry berikutnya. |
| `3` | **Exceeded Retry** | Gagal terus-menerus hingga mencapai batas maksimal retry (default: 5 kali). Memerlukan penanganan manual. |

---

## 5. Batasan & Catatan Teknis Berdasarkan Kode Nyata

1. **Staging Database di Scheduler**:
   Meskipun Web API mendukung Staging DB jenis MySQL dan SQL Server, class [`ConfigService.BuildStagingConnectionString`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/ConfigService.cs) pada Scheduler saat ini **hanya mengembalikan connection string jika tipe DB adalah SQL Server**. Jika profil memakai MySQL, Scheduler akan menampilkan peringatan dan tidak memproses data.
2. **Sinkronisasi Service Layer**:
   Opsi `SyncServiceLayer` di UI dan konfigurasi masih berupa **stub** (belum ada kode eksekusi pemanggilan REST Service Layer).
3. **Penyimpanan Refresh Token**:
   Penyimpanan refresh token di Web API berada di dalam RAM (`RefreshTokenStore`). Jika Web API direstart, seluruh refresh token yang belum kedaluwarsa akan hilang dan klien harus meminta token baru menggunakan `client_credentials`.
4. **Pengiriman Konfigurasi Profil (ProfileSync)**:
   Saat profil diubah di UI Scheduler, konfigurasi dikirim ke endpoint `POST /api/ProfileSync` Web API tanpa header Bearer Token. Jika menggunakan protokol HTTP biasa, kredensial database dikirim dalam format plain text XML. Direkomendasikan menggunakan URL HTTPS di lingkungan produksi.
