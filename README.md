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

### 2. Purchase Order
- **`POST /api/PurchaseOrder`**
  - Mengirim transaksi PO baru ke antrean staging. Dilengkapi proteksi idempotensi duplikasi.
- **`GET /api/PurchaseOrder/status/{webTxNumber}`**
  - Cek status on-demand dokumen (mengetahui apakah masih `Pending`, `Failed`, atau sudah `Success` di SAP).
- **`GET /api/PurchaseOrder/sync-status?since={isoDateTime}`**
  - Batch rekonsiliasi data status untuk menarik semua perubahan status transaksi sejak waktu tertentu.
- **`DELETE /api/PurchaseOrder/{webTxNumber}`**
  - Membatalkan dan menghapus dokumen yang masih berada di antrean Staging Hub (belum masuk SAP).
- **`POST /api/PurchaseOrder/cancel`**
  - Memproses permintaan pembatalan dokumen yang sudah berhasil di-post ke SAP B1.

---

## 🛡️ Keamanan & Kredensial
- Kredensial default dalam file konfigurasi development adalah contoh/placeholder untuk lingkungan staging lokal.
- Untuk deployment produksi, gunakan Environment Variables untuk `JWT_SIGNING_KEY` dan `CLIENT_SECRET`.
- Jangan commit kredensial produksi ke repository publik.

---

## 📄 Lisensi
Hak Cipta © 2026 SOLTIUS / PT Metrodata Electronics Tbk. Seluruh hak cipta dilindungi undang-undang.
