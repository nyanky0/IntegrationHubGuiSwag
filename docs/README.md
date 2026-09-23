# Dokumentasi Resmi SOLTIUS Integration Hub API

Selamat datang di repositori dokumentasi resmi **SOLTIUS - Web API Add-On (Integration Hub)** untuk ekosistem pengadaan barang armada kapal **IBT ONE Web App**.

---

## 📚 Daftar Dokumen Tersedia

1. **[DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)**:
   - Panduan arsitektur middleware staging gateway.
   - Ketetapan batas cakupan domain (Scope Web App vs SAP B1).
   - Daftar lengkap endpoint Transaksi dan Master Data.
   - Konvensi dual-database (SQL Server & SAP HANA) dan mock fallback.
2. **[SAP_B1_CUSTOMIZATION_CHECKLIST.md](SAP_B1_CUSTOMIZATION_CHECKLIST.md)**:
   - Daftar User-Defined Table (UDT) Staging dan Master Data.
   - Spesifikasi User-Defined Fields (UDF) untuk tabel `@SOL_ITEMHIER` dan tabel staging transaksi.
   - Prosedur restart SAP Service Layer.
   - Perintah verifikasi skema database via `sqlcmd`.

---

## 📌 Ringkasan Penting Domain Scope Web App

- **RL (Requisition Letter)**: Murni proses internal Web App, **TIDAK ADA di SAP B1**.
- **Logistik & Penerimaan Barang**: Menggunakan alur **GRPO** (Goods Receipt PO) dan **Stock Transfer** (IT). Tidak ada Goods Issue mandiri atau Goods Receipt mandiri di pengadaan web.
- **Keuangan & Faktur**: Pembayaran uang muka (AP DP) dan Faktur Pajak/Tagihan (AP Invoice) **TIDAK DIKELOLA di Web App**, melainkan diurus langsung oleh Finance di SAP B1 Client.
- **Modul Dihapus (Takeout)**: Withholding Tax, Chart of Accounts, Dimensions, dan Departments (digabung ke Cost Center Dimensi 1).
- **Katalog Mesin Kapal**: Jalur pembuatan nomor kelompok item via `POST /b1s/v1/ItemGroups` (P7) tetap aktif 100%.
