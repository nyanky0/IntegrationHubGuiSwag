# 📚 Dokumentasi SOLTIUS Add-On (Staging 2026 Auth)

Folder ini berisi dokumentasi teknis dan panduan operasional lengkap untuk solusi **`SOLTIUS - Scheduler Add-On.sln`** yang mencakup dua project:
1. **SOLTIUS - Web API Add-On** (.NET 8.0 ASP.NET Core)
2. **SOLTIUS - Scheduler Add-On** (.NET Framework 4.8 WinForms & Windows Service)

---

## 📑 Daftar Isi Dokumentasi

| No | Dokumen | Ringkasan Isi |
| :---: | :--- | :--- |
| 1 | [`01_Penjelasan_Kedua_Project.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/01_Penjelasan_Kedua_Project.md) | **Ikhtisar & Peran Kedua Project**: Penjelasan arsitektur, perbedaan target framework, struktur direktori, pustaka pihak ketiga, dan relasi komunikasi antar project. |
| 2 | [`02_Cara_Kerja_Aplikasi.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/02_Cara_Kerja_Aplikasi.md) | **Cara Kerja Aplikasi Saat Ini**: Alur end-to-end data transaksi, autentikasi OAuth2/JWT, inisialisasi tabel otomatis, channel audit log, mekanisme polling staging, sinkronisasi SAP DI API COM, retry logic, serta status `process_status`. |
| 3 | [`03_Panduan_Penggunaan_Aplikasi.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/03_Panduan_Penggunaan_Aplikasi.md) | **Panduan Penggunaan Lengkap**: Prasyarat sistem, konfigurasi environment variable, cara menjalankan Web API, panduan request token & submit order, cara login & kelola profil SAP di Scheduler, sinkronisasi manual/dry run, filter log, retry failed data, ekspor Excel, dan instalasi Windows Service. |
| 4 | [`01_KondisiSaatIni.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/01_KondisiSaatIni.md) | **Audit & Analisis Teknis**: Analisis temuan kode, perbedaan kode vs spesifikasi awal (GUIDE.md), serta daftar saran perbaikan dan roadmap pengembangan. |
| 5 | [`CHANGELOG.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/CHANGELOG.md) | **Catatan Riwayat Perubahan**: Rekam jejak pembaruan fitur, perbaikan bug, dan pembaruan arsitektur dari waktu ke waktu. |

---

## 🧭 Urutan Membaca yang Disarankan
1. **Untuk Developer Baru**: Mulai dari [`01_Penjelasan_Kedua_Project.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/01_Penjelasan_Kedua_Project.md) lalu lanjut ke [`02_Cara_Kerja_Aplikasi.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/02_Cara_Kerja_Aplikasi.md).
2. **Untuk Operator / QA / Tester**: Buka langsung [`03_Panduan_Penggunaan_Aplikasi.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/03_Panduan_Penggunaan_Aplikasi.md).
3. **Untuk Lead / Tech Reviewer**: Tinjau [`01_KondisiSaatIni.md`](file:///D:/Metrodata/IBT/Template_Addon_Staging_2026_Auth%202/Template_Addon_Staging_2026_Auth/Documentation/01_KondisiSaatIni.md).