# 📝 CHANGELOG — SOLTIUS Add-On (Staging 2026 Auth)

> Log perubahan/update project selama development.
> Format: **tanggal — deskripsi perubahan (project / file yang berubah)**.
> Baru = paling atas. Update file ini SETIAP ada perubahan di project.

---

## [Unreleased / Baseline]

### 2026-09-07 — Feat: Implementasi Modul Purchase Order (OPOR) End-to-End & Recovery Force Close
- **Recovery Force Close**: Membersihkan cache build yang korup akibat pemadaman mendadak/force close (`DefineStaticWebAssets` invalid byte 0x00) melalui `dotnet clean`.
- **Web API**: [`Controllers/PurchaseOrderController.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers/PurchaseOrderController.cs) — Menambahkan REST API endpoint `POST /api/PurchaseOrder` untuk menerima payload Purchase Order (Header & Lines).
- **Web API**: [`Models/Transaction/PurchaseOrderHeader.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Models/Transaction/PurchaseOrderHeader.cs) & [`Models/Transaction/PurchaseOrderDetail.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Models/Transaction/PurchaseOrderDetail.cs) — Model DTO Purchase Order dengan validasi data anotasi.
- **Web API**: [`Repositories/IPurchaseOrderRepository.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Repositories/IPurchaseOrderRepository.cs) & [`Repositories/PurchaseOrderRepository.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Repositories/PurchaseOrderRepository.cs) — Implementasi penyimpanan transaksi ke database staging SQL Server / MySQL ke tabel `SOL_PURCHASE_ORDER_HEADER` dan `SOL_PURCHASE_ORDER_DETAIL`.
- **Web API**: [`Services/IPurchaseOrderService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/IPurchaseOrderService.cs) & [`Services/PurchaseOrderService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/PurchaseOrderService.cs) — Business logic service untuk Purchase Order.
- **Web API**: [`Database/Initializers/SqlServerDatabaseInitializer.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Database/Initializers/SqlServerDatabaseInitializer.cs) & [`Database/Initializers/MySqlDatabaseInitializer.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Database/Initializers/MySqlDatabaseInitializer.cs) — Inisialisasi otomatis tabel staging PO (`SOL_PURCHASE_ORDER_HEADER`, `SOL_PURCHASE_ORDER_DETAIL`).
- **Web API**: [`Program.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Program.cs) — Registrasi dependency injection untuk `IPurchaseOrderService` dan `IPurchaseOrderRepository`.
- **Scheduler**: [`Model/PendingPurchaseOrder.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Model/PendingPurchaseOrder.cs) — Model data pending PO dari staging database.
- **Scheduler**: [`Services/DatabaseService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/DatabaseService.cs) — Penambahan method `LoadPendingPurchaseOrders()`, `UpdatePurchaseOrderStatus(...)`, `IsPurchaseOrderRetryLimitExceeded(...)`, dan `MarkPurchaseOrderAsExceededRetryLimit(...)`.
- **Scheduler**: [`Services/SapSyncService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SapSyncService.cs) — Penambahan method `ExecutePurchaseOrderSync(...)` menggunakan SAP B1 DI API COM object `BoObjectTypes.oPurchaseOrders` (OPOR) dengan pengelolaan pelepasan COM object defensif.
- **Scheduler**: [`Services/PurchaseOrderSyncRunner.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/PurchaseOrderSyncRunner.cs) — Background headless runner untuk eksekusi sinkronisasi Purchase Order lengkap dengan pengecekan batas retry, penanganan dry-run, audit logging, dan dead-letter handling.
- **Scheduler**: [`Model/SchedulerConfig.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Model/SchedulerConfig.cs) & [`Services/SyncSchedulerEngine.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/Services/SyncSchedulerEngine.cs) — Dukungan konfigurasi `SyncPurchaseOrder` untuk background scheduler loop dan Windows Service.
- **Scheduler**: [`UI/FormSettingScheduler.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormSettingScheduler.cs) — Checkbox "Sync Purchase Order (OPOR)" pada dialog pengaturan scheduler.
- **Scheduler**: [`UI/FormMain.Designer.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormMain.Designer.cs) & [`UI/FormMain.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormMain.cs) — Checkbox `chkPO` (-OPOR) pada form sinkronisasi utama, filter "Purchase Order" pada combobox log, eksekusi sinkronisasi manual/dry-run PO, dan dukungan retry gagal untuk PO.
- **Scheduler**: [`SOLTIUS - Scheduler Add-On.csproj`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/SOLTIUS%20-%20Scheduler%20Add-On.csproj) — Mendaftarkan file-file baru ke build project .NET Framework 4.8.

### 2026-09-07 — Fix: Popup Ganda Saat Choose Profil & Manage Profile Terbuka Dua Kali
- **Scheduler**: [`UI/FormChooseCF.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormChooseCF.cs) & [`UI/FormChooseCF.Designer.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormChooseCF.Designer.cs) — Memperbaiki event handler tombol `button1` (Choose) dan `button2` (Manage Profile) yang terdaftar ganda (di-subscribe di `InitializeComponent()` dan di-subscribe ulang di constructor). Menggunakan proteksi unsub/resub (`-=` lalu `+=`) agar delegate event tidak terduplikasi.
- **Scheduler**: [`UI/FormChooseCF.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormChooseCF.cs) — Menghapus `MessageBox.Show` berlebih di dalam dialog `FormChooseCF.button1_Click`, mengekspos properti `SelectedProfileName`, dan memperbarui `button3_Click` agar membungkus `FormCreate` dengan `using` serta memanggil `RefreshDataSetelahEdit()`.
- **Scheduler**: [`UI/FormMain.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormMain.cs) — Mengonsolidasikan notifikasi popup saat memilih profil menjadi satu kali konfirmasi informatif: `Profil '{activeName}' aktif digunakan & konfigurasi berhasil dikirim ke Web Server!`.
- **Scheduler**: [`UI/ManageProfile.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/ManageProfile.cs) & [`UI/FormCreate.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Scheduler%20Add-On/UI/FormCreate.cs) — Menerapkan unsubscribe/resubscribe defensif pada event `Load` dan button click untuk mencegah kemungkinan duplikasi handler di masa mendatang.
- **Web API**: [`Program.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Program.cs) — Mendukung pembacaan `JWT_SIGNING_KEY` dan `CLIENT_SECRET` langsung dari environment variables dan menyediakan fallback otomatis saat mode `Development` sehingga server tidak langsung crash saat dijalankan via Visual Studio (F5) / `dotnet run`.
- **Web API**: [`Properties/launchSettings.json`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Properties/launchSettings.json) & [`appsettings.Development.json`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/appsettings.Development.json) — Menambahkan konfigurasi development default (`NYANKYO`, `sa`, `P@ssw0rd`, development JWT key & secret).
- **Web API**: [`Controllers/ProfileSyncController.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Controllers/ProfileSyncController.cs) — Menambahkan atribut `[AllowAnonymous]` agar aplikasi Scheduler Add-On dapat mengirimkan konfigurasi XML profil database staging tanpa tertolak `401 Unauthorized`.
- **Web API**: [`Services/Configuration/ConfigurationService.cs`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/SOLTIUS%20-%20Web%20API%20Add-On/Services/Configuration/ConfigurationService.cs) — Memperbaiki fungsi `ConfigureAsync(string xml)` agar mem-parsing payload XML yang baru dikirimkan, bukan membaca file kosong di disk sebelum disimpan (memperbaiki error 500 saat initial setup).
- **Testing**: Terverifikasi end-to-end (Health Check 200, OAuth2 Token 200, ProfileSync XML 200, Status 200 Ready, dan Sales Order insert ke database staging `NYANKYO`).
- **Docs**: Dibuat [`01_Penjelasan_Kedua_Project.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/01_Penjelasan_Kedua_Project.md) — Penjelasan detail arsitektur, perbedaan framework, struktur direktori, dan relasi komunikasi antara Web API Add-On dan Scheduler Add-On.
- **Docs**: Dibuat [`02_Cara_Kerja_Aplikasi.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/02_Cara_Kerja_Aplikasi.md) — Alur teknis end-to-end data transaksi, autentikasi OAuth2/JWT, inisialisasi tabel, background logging, polling staging DB, DI API COM SAP B1, update status, dan retry mechanism.
- **Docs**: Dibuat [`03_Panduan_Penggunaan_Aplikasi.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/03_Panduan_Penggunaan_Aplikasi.md) — Panduan lengkap step-by-step setup database, konfigurasi env variable, menjalankan Web API, submit order via curl, panduan login & profil di Scheduler, sinkronisasi manual/dry-run, filter log, retry failed data, ekspor Excel, dan manajemen Windows Service.
- **Docs**: Diperbarui [`README.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/README.md) sebagai master navigasi dokumentasi.

### 2026-09-04 — UX: tekan Enter di field password = Login
- **Scheduler**: `FormLogin` — tambah handler `txtPassword_KeyDown`; tekan Enter di field password memicu `btnLogin.PerformClick()` (sama seperti klik tombol Sign In). `e.SuppressKeyPress` mencegah bunyi "ding" bawaan Windows.

### 2026-09-04 — Security: scrub kredensial dari repo public
- **Web API**: `appsettings.json` — kredensial DB asli diganti placeholder env var (`${STAGING_DB_SERVER}`, `${STAGING_DB_USER}`, `${LOG_DB_*}`) supaya aman di repo public.
- **Web API**: `Configuration/Config.xml` & root `Configuration/Config.xml` — isi kredensial dikosongkan (diisi otomatis via `POST /api/ProfileSync` dari Scheduler).
- **Docs**: `01_KondisiSaatIni.md` — hapus contoh string password plaintext dari teks.
- ⚠️ **PENTING**: kredensial lama (sudah pernah ke-push ke repo public) harus dianggap bocor → ganti password DB & rotate credential.

### 2026-09-04 — Fix: retry logic, quantity desimal, cleanup repo
- **Scheduler**: Perbaiki retry logic di `SalesOrderSyncRunner` — hapus `GetCurrentRetryCount()` yang selalu return 0 & `GetMaxRetryCount()` hardcoded; sekarang update status dulu lalu cek `IsRetryLimitExceeded` → dead-letter langsung di cycle yang sama.
- **Scheduler**: `SyncLogModel.Quantity` diubah `int` → `double` (di `Model/Model.cs`, `DatabaseService`, `FormMain`) — quantity desimal tidak ter-truncate lagi.
- **Scheduler**: Kolom `Quantity` di `TBL_SYNC_HISTORY` & `TBL_SYNC_ERROR` diubah `INT` → `DECIMAL(18,2)` (berlaku untuk tabel baru; tabel lama perlu ALTER manual).
- **Repo**: `build_verify/` (42MB DLL hasil build) di-ignore & dikeluarkan dari tracking.
- **Repo**: Hapus baris ignore `/SOLTIUS - Scheduler Add-On/UI/FormMain.cs` di `.gitignore` — FormMain.cs ikut ke-commit.
- **Docs**: Ditambahkan `README.md` di root.

### 2026-09-04 — Dokumentasi awal dibuat
- **Docs**: Dibuat folder `Documentation/` berisi:
  - `README.md` (daftar isi)
  - `01_KondisiSaatIni.md` (cara kerja program saat ini + saran kekurangan)
  - `CHANGELOG.md` (file ini)
- Tidak ada perubahan kode — murni dokumentasi kondisi baseline.

---

## Template entri baru (copy-paste di atas baris ini)

```markdown
### YYYY-MM-DD — <judul singkat perubahan>
- **<Web API | Scheduler | Docs>**: <deskripsi perubahan>
- **File**: <path file yang berubah>
- <catatan tambahan / alasan kalau perlu>
```
