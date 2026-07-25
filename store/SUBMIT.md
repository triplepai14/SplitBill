# TallyMore — Play Store submission checklist

Everything below is ready in this repo unless marked **[you]** (needs an
action only you can do) or **[missing]**.

## App facts (copy as-is)
| Field | Value |
|---|---|
| App name | **TallyMore** |
| Package name | **com.magsenium.tallymore** (permanent — cannot change later) |
| Version name | 1.0 |
| Version code | 1 |
| Category | Finance (or Tools) |
| Contact email | triplepai14@gmail.com |
| Developer/publisher | Magsenium |

## Files to upload
| Asset | Path | Status |
|---|---|---|
| App bundle (.aab) | `bin/Release/net10.0-android/publish/com.magsenium.tallymore-Signed.aab` | ready |
| App icon 512×512 | `store/icon-512.png` | ready |
| Feature graphic 1024×500 | `store/feature-1024x500.png` | ready |
| Phone screenshots (4) | `store/screenshots/` | ready (1080×2316) |
| Store text (short/full, TH+EN) | `store/listing.md` | ready |
| Privacy policy | `docs/privacy-policy.html` | ready |

## Step by step (Play Console)

1. **[you]** Create app: Play Console → Create app → name "TallyMore",
   language, "App", "Free".

2. **[you]** Enable Play App Signing (default). Google holds the app
   signing key; our `splitbill.keystore` becomes the *upload key*. Keep
   backing it up — you sign every upload with it.

3. Upload the bundle: Testing → **Internal testing** → Create release →
   drop the `.aab`. (Internal testing needs no full review — quick way to
   get an install link.)

4. Store listing → paste from `store/listing.md`:
   - Short + full description (TH and/or EN)
   - Upload `store/icon-512.png` (app icon)
   - Upload `store/feature-1024x500.png` (feature graphic)
   - Upload phone screenshots (≥2, min side 320px, ratio 9:16)

5. **[you]** Privacy policy URL: enable GitHub Pages first —
   repo → Settings → Pages → Deploy from a branch → `main` + `/docs`.
   Then paste `https://triplepai14.github.io/SplitBill/privacy-policy.html`.
   (Branch must be merged to main first.)

6. Data safety form → answer:
   - Does the app collect or share user data? **No**
   - Data encrypted in transit? N/A (no data leaves the device)
   - Users can request deletion? Data is on-device only; uninstall removes it.

7. Content rating questionnaire → no sensitive content → expect **Everyone**.

8. Target audience: 13+ (or all ages — no data collected).

9. **[you]** Path to production (new personal accounts): Google requires
   **Closed testing with ≥12 testers for 14 days** before you can promote
   to Production. Plan testers now. (Company accounts skip this.)

## Screenshots (ready in store/screenshots/)
- `01-home.png` — bills list
- `02-result.png` — a bill's split result (items + each share)
- `03-categories.png` — categories tab
- `04-category.png` — category summary with whole-category settle up
