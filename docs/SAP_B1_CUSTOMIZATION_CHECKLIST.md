# Checklist Kustomisasi & Skema Database SAP Business One

Dokumen ini adalah panduan teknis bagi Konsultan / Administrator Database SAP B1 dalam mempersiapkan objek kustomisasi (UDT, UDF, UDO) yang dibutuhkan oleh **SOLTIUS Integration Hub** dan **IBT ONE Web App**.

---

## 1. User-Defined Tables (UDT)

Buka SAP Business One Client dengan hak akses superuser:
> **Menu**: *Tools $\to$ Customization Tools $\to$ User-Defined Tables - Setup*

### 1.1 Tabel Staging Transaksi

| Nama Tabel | Deskripsi | Tipe Objek |
| :--- | :--- | :---: |
| `SOL_PO_H` | Staging Header Purchase Order | `No Object` |
| `SOL_PO_D` | Staging Detail Lines Purchase Order | `No Object` |
| `SOL_GRPO_H` | Staging Header Goods Receipt PO | `No Object` |
| `SOL_GRPO_D` | Staging Detail Lines Goods Receipt PO | `No Object` |
| `SOL_IT_H` | Staging Header Inventory Transfer | `No Object` |
| `SOL_IT_D` | Staging Detail Lines Inventory Transfer | `No Object` |

### 1.2 Tabel Master Data Kustom

| Nama Tabel | Deskripsi | Tipe Objek | Keterangan |
| :--- | :--- | :---: | :--- |
| `SOL_ITEMHIER` | Item Hierarchy Pohon Kelompok Barang | `Master Data` / `No Object` | Pengganti modul viewer kelompok barang |
| `SOL_PNUM_H` | Part Number Master Header | `Master Data` | Didampingi UDO `SOL_PNUM_H` |
| `SOL_PNUM_D` | Part Number Master Detail Lines | `Master Data Lines` | Baris nomor komponen / sparepart |

---

## 2. User-Defined Fields (UDF) Wajib per Tabel

Buka:
> **Menu**: *Tools $\to$ Customization Tools $\to$ User-Defined Fields - Management*

### 2.1 Field pada Tabel `@SOL_ITEMHIER`
| Field Name | Deskripsi | Tipe Data | Ukuran | Validasi / Nullable |
| :--- | :--- | :--- | :---: | :--- |
| `U_SOL_Code` | Kode Node Hierarchy | Alphanumeric | 50 | Mandatory, Unik |
| `U_SOL_Name` | Deskripsi / Nama Node | Alphanumeric | 100 | Mandatory |
| `U_SOL_Parent` | Kode Parent Node | Alphanumeric | 50 | Nullable (Root = NULL) |
| `U_SOL_CostCenter` | Kode Cost Center Armada | Alphanumeric | 50 | Nullable |
| `U_SOL_Ownership` | Kepemilikan Operasional | Alphanumeric | 50 | Nullable (e.g. 'Operation') |
| `U_SOL_Level` | Level Kedalaman Pohon | Numeric | 4 | Mandatory (Root = 1) |

---

### 2.2 Field Status Staging pada Header Transaksi (`@SOL_PO_H`, `@SOL_GRPO_H`, `@SOL_IT_H`)
| Field Name | Deskripsi | Tipe Data | Nilai Default | Keterangan Nilai |
| :--- | :--- | :--- | :---: | :--- |
| `U_SOL_WebTxNum` | Nomor Transaksi Web App | Alpha (50) | - | Kunci referensi unik dokumen web |
| `U_SOL_ProcessStatus` | Status Eksekusi Add-On | Numeric (2) | `0` | `0`: Staged, `1`: Sukses di SAP, `2`: Gagal/Error |
| `U_SOL_SapDocNum` | Nomor Dokumen Resmi SAP | Alpha (50) | - | Nomor dokumen SAP (DocNum) setelah terposting |
| `U_SOL_SapDocEntry` | DocEntry SAP | Numeric (11) | - | Primary Key DocEntry pada tabel SAP resmi |
| `U_SOL_ErrorMessage` | Pesan Log Error DI API | Alpha (254) | - | Pesan kegagalan dari SAP DI API jika status = 2 |

---

## 3. Prosedur Restart Service Layer (Wajib Setelah Pembuatan UDT/UDO)

Metadata Service Layer dibentuk saat startup service. Jika UDT `@SOL_ITEMHIER` atau UDO baru dibuat, jalankan restart service:

### Di Lingkungan Linux (SUSE / RHEL - HANA):
```bash
systemctl restart b1s
systemctl status b1s
```

### Di Lingkungan Windows Server (SQL Server):
1. Buka `services.msc`.
2. Cari service **SAP Business One Service Layer**.
3. Klik **Restart**.

---

## 4. Skrip Verifikasi Database Tanpa Membuka SAP Client

Gunakan utilitas command line `sqlcmd` untuk memverifikasi apakah tabel dan kolom telah terbentuk di database:

```powershell
# Cek keberadaan tabel kustom
sqlcmd -S localhost -E -d IBTWEBAPP -Q "SELECT TableName, Descr FROM OUTB WHERE TableName LIKE 'SOL_%';"

# Cek field kustom pada tabel @SOL_ITEMHIER
sqlcmd -S localhost -E -d IBTWEBAPP -Q "SELECT AliasID, Descr, TypeID, EditSize FROM CUFD WHERE TableID = '@SOL_ITEMHIER';"
```
