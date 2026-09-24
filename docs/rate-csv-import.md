# Rate CSV imports

Both import endpoints accept a multipart form field named `file`, containing a UTF-8 `.csv` file. Imports are validated as a whole: if any row is invalid, the response identifies the row and no rows are written. Files are limited to 2 MiB and 5,000 data rows. Rates and charges are recorded in the audit log with the authenticated user's ID.

After T8 adds authentication, rate and local-charge imports require an `Operation` or `Admin` role. Writes return `401` until an authenticated identity with a numeric `NameIdentifier` claim is configured. Master-data writes require `Admin`.

## Freight rates

Endpoint: `POST /api/rates/import`

Required columns:

```csv
origin_code,destination_code,mode,direction,carrier_code,price_min,price_max,currency_code,valid_from,valid_to
```

Optional columns: `container_size`, `weight_break_min`, `weight_break_max`.

Example row (illustrative only; referenced master data must already exist):

```csv
THLCH,CNNGB,Lcl,Import,EXAMPLE-LINE,12.50,15.00,USD,2026-10-01,2026-12-31
```

`mode` accepts `Fcl`, `Lcl`, or `Air`; `direction` accepts `Import` or `Export`. FCL requires `container_size`; LCL has no container or weight break; Air requires a weight break and cannot exceed 500 kg. Air weight breaks include their lower bound and exclude their upper bound, except that 500 kg is included in the final break. `valid_from` and `valid_to` use inclusive `YYYY-MM-DD` dates. Active rates matching the same route, carrier, slot, and validity window are updated in place with the newly imported prices.

## Local charges

Endpoint: `POST /api/local-charges/import`

Required columns:

```csv
port_code,direction,mode,charge_type,calc_basis,amount_min,amount_max,currency_code,charge_side
```

Optional column: `minimum_charge`.

Example row (illustrative only; referenced master data must already exist):

```csv
THLCH,Import,Lcl,CFS,PerRevenueTon,12.50,15.00,USD,Origin
```

`calc_basis` accepts `PerShipment`, `PerContainer`, `PerRevenueTon`, `PerCBM`, `PerKG`, or `NotQuotable`; `charge_side` accepts `Origin` or `Destination`. Use a dot for decimal fractions and omit thousands separators. Existing charges matching the same natural key (port, direction, mode, charge type, basis, side, currency) are updated in place with the newly imported amounts instead of being rejected as duplicates.
