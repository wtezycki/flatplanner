# Ingest report

## Fetched

Total: 198

| city | market | n |
|---|---|---|
| gdansk | primary | 54 |
| gdansk | secondary | 54 |
| gdynia | primary | 30 |
| gdynia | secondary | 29 |
| sopot | primary | 15 |
| sopot | secondary | 15 |
| unknown | unknown | 1 |

## Rejected

| reason | n |
|---|---|
| not_flat_sale | 1 |

## Fill-rate

Share of listings with a value, `n (%)`.

| column | gdansk | gdynia | sopot | total |
|---|---|---|---|---|
| district_slug | 108/108 (100%) | 59/59 (100%) | 30/30 (100%) | 197/197 (100%) |
| district_name | 108/108 (100%) | 59/59 (100%) | 30/30 (100%) | 197/197 (100%) |
| street | 80/108 (74.1%) | 57/59 (96.6%) | 29/30 (96.7%) | 166/197 (84.3%) |
| floor | 93/108 (86.1%) | 59/59 (100%) | 30/30 (100%) | 182/197 (92.4%) |
| building_floors | 86/108 (79.6%) | 33/59 (55.9%) | 27/30 (90%) | 146/197 (74.1%) |
| build_year | 101/108 (93.5%) | 58/59 (98.3%) | 30/30 (100%) | 189/197 (95.9%) |
| finish_condition | 102/108 (94.4%) | 57/59 (96.6%) | 30/30 (100%) | 189/197 (95.9%) |
| ownership | 101/108 (93.5%) | 57/59 (96.6%) | 22/30 (73.3%) | 180/197 (91.4%) |
| rent_pln | 40/108 (37%) | 25/59 (42.4%) | 13/30 (43.3%) | 78/197 (39.6%) |
| has_lift | 77/108 (71.3%) | 43/59 (72.9%) | 9/30 (30%) | 129/197 (65.5%) |
| has_balcony | 78/108 (72.2%) | 41/59 (69.5%) | 23/30 (76.7%) | 142/197 (72.1%) |
| has_garage | 81/108 (75%) | 39/59 (66.1%) | 15/30 (50%) | 135/197 (68.5%) |
| has_basement | 71/108 (65.7%) | 38/59 (64.4%) | 9/30 (30%) | 118/197 (59.9%) |
| advertiser_type | 107/108 (99.1%) | 59/59 (100%) | 30/30 (100%) | 196/197 (99.5%) |

## Flags

| flag | n |
|---|---|
| rent_zero_as_missing | 82 |
| floor_higher_10 | 5 |
| price_outlier | 4 |
| unknown_value:advertiser_type | 1 |

## Rent (czynsz)

Listings with `Rent = 0` stored as missing (`rent_zero_as_missing`).

| market | zero / all | share |
|---|---|---|
| primary | 67/99 | 67.7% |
| secondary | 15/98 | 15.3% |

## Finish condition by market

| finish_condition | primary | secondary |
|---|---|---|
| ready_to_use | 1 | 75 |
| to_completion | 93 | 12 |
| to_renovation | 0 | 8 |
| (null) | 5 | 3 |

## Districts

| city | unique districts |
|---|---|
| gdansk | 26 |
| gdynia | 16 |
| sopot | 6 |

Districts with a single listing (manual review): gdansk/chelm, gdansk/kokoszki, gdansk/krakowiec--gorki-zachodnie, gdansk/mlyniska, gdansk/nowy-port, gdansk/orunia--sw.-wojciech--lipce, gdansk/osowa, gdansk/przymorze-male, gdansk/stogi, gdansk/suchanino, gdansk/tysiaclecia, gdansk/vii-dwor, gdansk/wejhera, gdansk/zaspa--mlyniec, gdansk/zaspa--rozstaje, gdynia/babie-doly, gdynia/dabrowa, gdynia/karwiny, gdynia/obluze, gdynia/orlowo, gdynia/pogorze, gdynia/pustki-cisowskie, gdynia/wzgorze-sw.-maksymiliana, sopot/karlikowo, sopot/osiedle-mickiewicza

## Median price per m²

| city | n | median price_per_m2 |
|---|---|---|
| gdansk | 108 | 12844 |
| gdynia | 59 | 11741 |
| sopot | 30 | 25450 |

## Duplicate candidates

Pairs: 74; listings in at least one pair: 52/197 (26.4%).

| external_id A | advertiser A | external_id B | advertiser B |
|---|---|---|---|
| 68605587 | agency | 68605186 | agency |
| 68605587 | agency | 68604537 | agency |
| 68605587 | agency | 68603777 | agency |
| 68605587 | agency | 68603132 | agency |
| 68605587 | agency | 68602252 | agency |
| 68605566 | agency | 68605001 | agency |
| 68605566 | agency | 68603406 | agency |
| 68605466 | agency | 68604287 | agency |
| 68605466 | agency | 68603577 | agency |
| 68605466 | agency | 68603197 | agency |

## Primary market in the cheap ranking

Primary share overall: 50.3% (99/197); among the 20 lowest `price_per_m2 / city median`: 40% (8/20).
