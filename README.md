# Integration Hub: SOLTIUS Web API & Scheduler Add-On (GUI & Swagger)

Integration Hub adalah solusi middleware enterprise yang menjembatani komunikasi transaksi antara aplikasi client eksternal (seperti Web App IBT Laravel/React) dengan **SAP Business One** menggunakan arsitektur **Staging Database**, **Web API (.NET 8)** berbasis **OAuth2 & Swagger**, dan **Scheduler Add-On (.NET Framework 4.8 / WinForms GUI)** berbasis **SAP DI API (COM)**.

---

## 🏗️ Arsitektur Sistem

```
┌───────────────────────────┐
│     Client Web App        │
│   (IBT Laravel/React)     │
└─────────────┬─────────────┘
              │ 1. POST /api/PurchaseOrder (Bearer JWT)
              ▼
┌───────────────────────────┐
│   SOLTIUS Web API Hub     │
│   (.NET 8 - Port 5006)    │
│   Swagger UI & OAuth2     │
└─────────────┬─────────────┘
              │ 2. Idempotent Insert / Status Check / Delete
              ▼
┌───────────────────────────┐
│     Staging Database      │
│     (SQL Server / MySQL)  │
│  SOL_PURCHASE_ORDER_*     │
└─────────────┬─────────────┘
              │ 3. Polling Data Pending (Status = 0)
              ▼
┌───────────────────────────┐
│  SOLTIUS Scheduler Add-On │
│  (WinForms GUI / Windows) │
└─────────────┬─────────────┘
              │ 4. Documents.Add() / Documents.Cancel()
              ▼
┌───────────────────────────┐
│    SAP Business One       │
│  (DI API - SAPbobsCOM)    │
└───────────────────────────┘
```

---

## 📦 Komponen Project

| Project | Teknologi | Deskripsi & Tanggung Jawab |
|---|---|---|
| **`SOLTIUS - Web API Add-On`** | .NET 8 (ASP.NET Core) | Endpoint REST API, OAuth2 Client Credentials & JWT Bearer token, Swagger UI interaktif, validasi transaksi idempotent, staging data storage, serta endpoint cek status on-demand & pembatalan. |
| **`SOLTIUS - Scheduler Add-On`** | .NET Framework 4.8 (WinForms) | Antarmuka desktop GUI, manajemen profil SAP & Staging, integrasi SAP Business One via DI API (`SAPbobsCOM`), background automatic sync, log viewer interaktif (Header + Detail dialog popup), dan ekspor Excel. |

---

## 🚀 Fitur Utama

### 1. Siklus Hidup Transaksi (Status Lifecycle)
- **`not synced`**: Dokumen dibuat di Web App, belum dikirim ke Integration Hub.
- **`sync to hub`**: Dokumen tersimpan di antrean Staging DB (`SOL_PROCESS_STATUS = 0`).
- **`sync to sap`**: Dokumen sukses diposting ke SAP B1 (`SOL_PROCESS_STATUS = 1`, `SOL_DOCENTRY` terisi).
- **`failed`**: Dokumen gagal diposting ke SAP B1 (`SOL_PROCESS_STATUS = 2`, `SOL_ERRORMESSAGE` terisi).
- **`cancelled`**: Dokumen dibatalkan (`SOL_PROCESS_STATUS = 3`).

### 2. Idempotensi & Proteksi Duplikasi
- Mencegah dokumen ganda masuk ke SAP. Jika `WebTxNumber` yang sama dikirim kembali:
  - Jika sudah di SAP: Mengembalikan HTTP `409 Conflict` membawa nomor DocEntry SAP.
  - Jika masih antrean: Mengembalikan HTTP `409 Conflict` menginfokan dokumen sedang menunggu proses.

### 3. Alur Pembatalan Dokumen ("Cancel Dokumen")
- **Kondisi Antrean Hub**: Endpoint `DELETE /api/PurchaseOrder/{webTxNumber}` menghapus data staging secara bersih.
- **Kondisi Sudah di SAP**: Endpoint `POST /api/PurchaseOrder/cancel` menerima pembatalan dan memperbarui status staging menjadi `Cancelled`.

### 4. Scheduler Desktop GUI Interaktif
- **Log History**: Menampilkan ringkasan level header dokumen.
- **Detail Dokumen Popup**: Mengklik dua kali (double-click) pada baris transaksi akan membuka form popup yang menampilkan data lengkap header, JSON UDF viewer (Kapal/Pengaju), serta daftar baris item transaksi.
- **Multi-Filter**: Filter berdasarkan rentang tanggal, status (`All`, `Success`, `Failed`, `Pending`, `Cancelled`), modul dokumen (`Purchase Order`, `Sales Order`), serta pencarian teks bebas.
- **Export to Excel**: Ekspor data log langsung ke file `.xlsx`.

---

## ⚙️ Panduan Menjalankan

### A. Prasyarat
- Windows OS (karena DI API `SAPbobsCOM.dll` adalah 32/64-bit COM component).
- .NET 8 SDK & .NET Framework 4.8 Runtime.
- SQL Server (database staging `NYANKYO` dan SAP B1 DB).
- SAP Business One Client / DI API terinstal pada mesin yang menjalankan Scheduler.

---

### B. Menjalankan Web API Add-On
1. Masuk ke folder project:
   ```bash
   cd "SOLTIUS - Web API Add-On"
   ```
2. Jalankan aplikasi:
   ```bash
   dotnet run --urls http://localhost:5006
   ```
3. Akses Swagger UI di browser:
   ```
   http://localhost:5006/swagger
   ```

---

### C. Menjalankan Scheduler Add-On (GUI)
1. Buka solusi `SOLTIUS - Scheduler Add-On.sln` di **Visual Studio 2022** (atau build menggunakan MSBuild x64).
2. Jalankan executable dari:
   ```
   SOLTIUS - Scheduler Add-On\bin\x64\Debug\SOLTIUS - Scheduler Add-On.exe
   ```
3. Buka menu **Configuration** untuk memastikan profil aktif terhubung ke SAP B1 dan DB Staging.
4. Centang **Sync Purchase Order** (atau Sales Order), lalu klik **Start Sync**.

---

## 📡 Daftar Endpoint API

### 1. Autentikasi OAuth2
- **`POST /oauth2/token`**
  - **Body**:
    ```json
    {
      "grant_type": "client_credentials",
      "client_id": "scheduler-addon",
      "client_secret": "SoltiusClientSecretSchedulerAddon2026!"
    }
    ```
  - **Response**: Mengembalikan `access_token` Bearer JWT.

---

### 2. Purchase Order Staging
- **`POST /api/PurchaseOrder`**: Mengirim transaksi PO baru ke antrean staging. Dilengkapi proteksi idempotensi duplikasi.
- **`GET /api/PurchaseOrder/status/{webTxNumber}`**: Cek status on-demand dokumen (`Pending`, `Failed`, atau `Success`).
- **`GET /api/PurchaseOrder/sync-status?since={isoDateTime}`**: Batch rekonsiliasi data status untuk menarik perubahan status transaksi sejak waktu tertentu.
- **`DELETE /api/PurchaseOrder/{webTxNumber}`**: Membatalkan dokumen yang masih berada di antrean Staging Hub.
- **`POST /api/PurchaseOrder/cancel`**: Memproses permintaan pembatalan dokumen yang sudah berhasil di-post ke SAP B1.

---

### 3. Goods Receipt PO (GRPO) Staging
- **`POST /api/GoodsReceiptPO`**: Mengirim penerimaan barang PO ke antrean staging (`@SOL_GRPO_H` & `@SOL_GRPO_D`).
- **`GET /api/GoodsReceiptPO/status/{webTxNumber}`**: Cek status penerimaan barang ke SAP.

---

### 4. Stock Transfer (IT) Staging
- **`POST /api/StockTransfer`**: Mengirim mutasi/transfer barang antar gudang & armada kapal (`@SOL_IT_H` & `@SOL_IT_D`).
- **`GET /api/StockTransfer/status/{webTxNumber}`**: Cek status proses transfer barang ke SAP.

---

### 5. Master Data Suite (Dual Engine: SQL Server & SAP HANA)
Endpoint master data dilengkapi otomatisasi **Staging Fallback Mode** untuk development:
- **`GET /api/MasterData/item-hierarchy`**: Pohon hierarki kelompok barang suku cadang kapal (`@SOL_ITEMHIER`).
- **`GET /api/MasterData/items`**: Master suku cadang kapal & katalog (`OITM`).
- **`GET /api/MasterData/business-partners`**: Master vendor armada (`OCRD`).
- **`GET /api/MasterData/warehouses`**: Master gudang darat & armada kapal (`OWHS`).
- **`GET /api/MasterData/taxes`** (alias: `vat-groups`): Kode pajak transaksi / PPN (`OVTG`).
- **`GET /api/MasterData/uoms`** (alias: `unit-of-measurements`): Satuan ukuran barang (`OUOM`).
- **`GET /api/MasterData/uom-groups`** (alias: `unit-of-measurement-groups`): Grup satuan konversi (`OUGP`).
- **`GET /api/MasterData/bin-locations`**: Lokasi rak per gudang (`OBIN`).
- **`GET /api/MasterData/cost-centers`** (alias: `profit-centers`): Cost center armada / kapal & departemen (`OPRC`).
- **`GET /api/MasterData/payment-terms`** (alias: `payment-terms-types`): Termin pembayaran vendor (`OCTG`).
- **`GET /api/MasterData/freight`** (alias: `additional-expenses`): Biaya ekspedisi & ongkos angkut (`OEXD`).
- **`GET /api/MasterData/projects`**: Kode proyek docking & overhaul (`OPRJ`).
- **`GET /api/MasterData/item-groups`**: Master grup item suku cadang katalog (`OITB`).
- **`GET /api/MasterData/hs-codes`**: Klasifikasi HS Code & lartas (`OCHS`).
- **`GET /api/MasterData/part-numbers`**: Master part number komponen mesin kapal (`@SOL_PNUM_H` / `@SOL_PNUM_D`).

---

## 📚 Dokumentasi Teknis Lengkap

Detail arsitektur, checklist database, dan standar pengkodean tersedia pada folder [`docs/`](docs/):
- **[Development Guide & Architecture (`docs/DEVELOPMENT_GUIDE.md`)](docs/DEVELOPMENT_GUIDE.md)**: Panduan arsitektur, domain scope, aturan wajib push git, dan standar versioning.
- **[SAP B1 Customization Checklist (`docs/SAP_B1_CUSTOMIZATION_CHECKLIST.md`)](docs/SAP_B1_CUSTOMIZATION_CHECKLIST.md)**: Checklist UDT, UDF, UDO, restart Service Layer, dan skrip verifikasi `sqlcmd`.

---

## 🛡️ Keamanan & Kredensial
- Kredensial default dalam file konfigurasi development adalah contoh/placeholder untuk lingkungan staging lokal.
- Untuk deployment produksi, gunakan Environment Variables untuk `JWT_SIGNING_KEY` dan `CLIENT_SECRET`.
- Jangan commit kredensial produksi ke repository publik.

---

## 🏷️ Versioning & Release Tracking
Proyek ini mengadopsi standar **Semantic Versioning (SemVer)**:
- **`v1.0.0`**: Rilis awal Web API .NET 8, OAuth2 JWT Bearer, Swagger UI, Purchase Order Staging, dan Scheduler WinForms GUI (DI API).
- **`v1.1.0`**: Debug tab, total transaction purge, row delete context menu, staging DB health indicator, auto-resolution [-5006], selective sync queue.
- **`v1.2.0`**:
  - Penambahan endpoint `GET /api/MasterData/item-hierarchy` (`@SOL_ITEMHIER`).
  - Penambahan seluruh master data pengadaan: `taxes`, `uoms`, `uom-groups`, `bin-locations`, `cost-centers`, `payment-terms`, `freight`, `projects`, `item-groups`, `hs-codes`, dan `part-numbers`.
  - Penegasan Authoritative Domain Scope (RL tidak ada di SAP, tidak ada GI/GR mandiri, tidak ada AP DP/Invoice di web).
  - Pembentukan struktur dokumentasi resmi pada folder `docs/`.
  - Penetapan protokol wajib push Git setiap ada perubahan.

---

## 📄 Lisensi
Hak Cipta © 2026 SOLTIUS / PT Metrodata Electronics Tbk. Seluruh hak cipta dilindungi undang-undang.

