# Troubleshooting Guide — Atlas Inventory

## Error INV-001: Stock update rejected

**Cause:** The requested stock quantity is negative.

**Resolution:** Enter a quantity greater than or equal to zero and submit the update again.

The application does not allow negative stock quantities.

## Error INV-002: Product not found

**Cause:** The supplied product identifier does not match an existing product.

**Resolution:** Verify the product identifier and confirm that the product exists in the selected warehouse.

## Error INV-003: CSV export failed

**Cause:** The user does not have permission to export reports, or the export service is temporarily unavailable.

**Resolution:** First, verify that the user's role allows report access. If the problem persists, retry the export after a few minutes.

## Error INV-004: Dashboard data appears outdated

**Cause:** The dashboard may not have completed its scheduled refresh.

**Resolution:** Manually refresh the dashboard. If the displayed information remains outdated, check the inventory service status.

## Important limitation

These troubleshooting instructions describe the fictional Atlas Inventory application. They should not be treated as instructions for a real inventory system.