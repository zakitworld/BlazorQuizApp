# Operations runbook

## Required secrets

Supply these with the hosting platform's secret store or environment variables. Never place them in tracked JSON files.

- `ConnectionStrings__DefaultConnection`
- `AppSettings__AdminEmail` and `AppSettings__AdminPassword` for first-run bootstrap only
- `EmailSettings__SendGridApiKey`
- `Paystack__PublicKey` and `Paystack__SecretKey`
- Paystack plan codes under `Paystack__PricingPlans__*`

Remove the bootstrap admin secrets after the account has been created. Rotate any secret that has been copied into logs, tickets, or source control.

## Deployment

1. Back up the database.
2. Generate and review an idempotent migration script with `dotnet ef migrations script --idempotent`.
3. Apply the script using a dedicated deployment identity; the web process should not normally have schema-alter permissions.
4. Deploy the application and verify `/health/live` publicly and `/health/ready` with an administrator session.
5. Confirm email delivery and a Paystack test transaction before accepting traffic.

## Backups and recovery

- Enable SQL Server automated full backups daily and transaction-log backups every 5–15 minutes where point-in-time recovery is required.
- Encrypt backups, retain copies in a separate account/region, and restrict restore permissions.
- Run a documented restore drill at least quarterly. A backup is not considered valid until a restore has been tested.

## Monitoring

Alert on repeated login failures, invalid webhook signatures, payment verification failures, HTTP 5xx responses, failed readiness checks, and database storage growth. Retain application logs according to the organization's privacy policy; audit records should be append-only to application users.
