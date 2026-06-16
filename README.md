# Alternate Component Verification Engine (DigiKey, Mouser, & LCSC API)

This application is an automated, data-driven **Alternate Part Verification Engine** built specifically for the Electronic Manufacturing Services (EMS) industry. It streamlines component sourcing and engineering validation by programmatically cross-referencing alternate part numbers against multi-distributor API data from **DigiKey**, **Mouser**, and **LCSC** to instantly determine compatibility.

## 🔄 Dynamic Sourcing & Verification Workflow

The system fetches parameters directly from global distributor networks to run in-place hardware validations:

1. **Multi-API Aggregation:** The engine runs parallel, authenticated API calls to fetch data sheets, descriptions, and item attributes from all three distributor catalogs simultaneously.
2. **Technical Attribute Extraction:** Normalizes divergent data forms into clear, comparable hardware metrics (e.g., verifying electrical ratings, case packaging, tolerance ranges).
3. **Compatibility Verdict:** The system compares the aggregated data points against your original hardware requirements to issue a final "Go / No-Go" verification status.

---

## 🌟 Key Application Features

* **Live Multi-Distributor Scraping:** Aggregates real-time descriptions, data fields, and specification sheets directly from DigiKey, Mouser, and LCSC without manual browser tracking.
* **Smart Parameter Scoring:** An intelligent parsing algorithm evaluates product descriptions to catch mismatches in critical attributes (such as tolerance shifts, voltage limits, or pin configurations).
* **Granular Hardware Matching:** Extracts and highlights primary electronic assembly constraints instantly:
  * **Package / Case Size Sizing** (cross-referencing standard Imperial vs. Metric layout metrics)
  * **Moisture Sensitivity Level (MSL)** * **Mounting Type** (SMT vs. Through-Hole footprint verification)
* **Status Analytics Dashboard:** Uses clear, color-coded visual queues indicating whether an alternate part is **Approved (Ok to Use)**, **Conditional (Check Specifications)**, or **Rejected (Incompatible)**.
* **Unified Export Matrix:** Allows engineering and procurement teams to download localized parameter reports into comprehensive Excel or PDF format files for build authorization logs.
* **Adaptive Dark / Light Layout:** Incorporates an integrated, persistent display theme wrapper optimizing system readability across different manufacturing environments.

---

## 🚀 Technical Architecture

* **Backend Framework:** ASP.NET Core MVC (C#)
* **Integration Layer:** HttpClient / RESTful API Integration with DigiKey, Mouser, and LCSC Developer Hubs
* **Database Management:** Entity Framework Core (Relational SQL Logging for part audit requests and compliance tracking)
* **Frontend Design:** Responsive HTML5, Bootstrap 5, Custom CSS Variables, and JavaScript (Local Storage Theme Persistence)
