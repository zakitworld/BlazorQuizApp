# Blazor Quiz App - SmartASP.NET Deployment Guide

## Prerequisites

Before deploying to SmartASP.NET, ensure you have:

1. **SmartASP.NET Hosting Account** with:
   - ASP.NET Core 9.0 support
   - SQL Server database
   - FTP access credentials

2. **External Services**:
   - SendGrid account and API key
   - Stripe account with API keys
   - Domain name (optional but recommended)

## Step 1: Configure External Services

### SendGrid Setup
1. Sign up at [SendGrid](https://sendgrid.com/)
2. Create an API key with "Mail Send" permissions
3. Verify your sender email address
4. Note your API key for configuration

### Stripe Setup
1. Sign up at [Stripe](https://stripe.com/)
2. Create products and pricing:
   - Premium Monthly: $9.99/month
   - Pro Monthly: $29.99/month
3. Note the Price IDs from Stripe Dashboard
4. Get your API keys (Publishable and Secret)
5. Set up webhook endpoint: `https://yourdomain.com/api/stripe/webhook`
6. Note the webhook signing secret

## Step 2: Prepare Database

### Create Database in SmartASP.NET
1. Log into SmartASP.NET control panel
2. Navigate to **SQL Server** section
3. Create a new database (e.g., `BlazorQuizApp`)
4. Note the connection string provided

### Update Connection String
The connection string format for SmartASP.NET:
```
Server=your-server.smarterasp.net;Database=DB_XXXXX_quizapp;User Id=DB_XXXXX_quizapp_admin;Password=your-password;TrustServerCertificate=True;
```

## Step 3: Configure Production Settings

### Update appsettings.Production.json

Replace placeholders with actual values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.smarterasp.net;Database=DB_XXXXX_quizapp;User Id=DB_XXXXX_quizapp_admin;Password=your-password;TrustServerCertificate=True;"
  },
  "AppSettings": {
    "AdminUser": {
      "Email": "your-admin@email.com"
    },
    "BaseUrl": "https://yourdomain.com"
  },
  "EmailSettings": {
    "SendGridApiKey": "SG.your-sendgrid-api-key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Quiz App"
  },
  "Stripe": {
    "PublishableKey": "pk_live_your-publishable-key",
    "SecretKey": "sk_live_your-secret-key",
    "WebhookSecret": "whsec_your-webhook-secret",
    "PricingIds": {
      "PremiumMonthly": "price_premium_id_from_stripe",
      "ProMonthly": "price_pro_id_from_stripe"
    }
  }
}
```

## Step 4: Build and Publish

### Build for Production

```powershell
# Navigate to project directory
cd "g:\From Desktop\Console,WinUI3, Blazor & MAUI\BlazorQuizApp"

# Build in Release mode
dotnet build -c Release

# Publish the application
dotnet publish -c Release -o ./publish
```

This creates a `publish` folder with all files needed for deployment.

## Step 5: Upload to SmartASP.NET

### Via FTP
1. Connect to SmartASP.NET FTP using FileZilla or similar:
   - Host: `ftp.yourdomain.com`
   - Username: Your FTP username
   - Password: Your FTP password
   - Port: 21

2. Navigate to the `wwwroot` folder (or your site's root)

3. Upload ALL files from the `publish` folder to the root directory

4. Ensure the following structure:
   ```
   wwwroot/
   ├── BlazorQuizApp.dll
   ├── appsettings.json
   ├── appsettings.Production.json
   ├── web.config
   ├── wwwroot/
   └── ... (other files)
   ```

## Step 6: Run Database Migrations

### Option 1: Using Local Migration (Recommended)

```powershell
# Update connection string in appsettings.json temporarily to production database
dotnet ef database update --connection "Server=your-server.smarterasp.net;Database=DB_XXXXX_quizapp;User Id=DB_XXXXX_quizapp_admin;Password=your-password;TrustServerCertificate=True;"
```

### Option 2: Automatic Migration on First Run
The application is configured to run migrations automatically on startup (see Program.cs).

## Step 7: Configure IIS in SmartASP.NET

1. Log into SmartASP.NET control panel
2. Navigate to **IIS Manager**
3. Ensure your site is configured for:
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated
   - Application Pool: .NET Core

4. Set environment variable:
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production`

## Step 8: SSL Certificate

1. In SmartASP.NET control panel, navigate to **SSL**
2. Install a free Let's Encrypt certificate or upload your own
3. Enable HTTPS redirection (already configured in Program.cs)

## Step 9: Test the Application

### Health Checks
Visit these URLs to verify deployment:
- `https://yourdomain.com/health` - Should return "Healthy"
- `https://yourdomain.com/health/ready` - Readiness check
- `https://yourdomain.com/health/live` - Liveness check

### Test User Registration
1. Navigate to `https://yourdomain.com`
2. Register a new account
3. Check email for verification link
4. Verify email works correctly

### Test Quiz Functionality
1. Log in with verified account
2. Take a quiz
3. Check leaderboard
4. Verify statistics page

### Test Payment Flow (Stripe Test Mode First!)
1. Navigate to `/pricing`
2. Select a plan
3. Use Stripe test card: `4242 4242 4242 4242`
4. Complete checkout
5. Verify subscription activation

## Step 10: Configure Stripe Webhook

1. In Stripe Dashboard, go to **Developers > Webhooks**
2. Add endpoint: `https://yourdomain.com/api/stripe/webhook`
3. Select events to listen for:
   - `checkout.session.completed`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
4. Copy the webhook signing secret
5. Update `appsettings.Production.json` with the secret

## Step 11: Create Admin User

The application will automatically create an admin role on first run. To assign admin to a user:

1. Register with the email specified in `AppSettings:AdminUser:Email`
2. The seeding process will automatically assign admin role
3. Log in and navigate to `/manage-questions` to verify admin access

## Monitoring and Maintenance

### Log Files
Logs are stored in the `logs` folder:
- Location: `wwwroot/logs/blazorquizapp-YYYYMMDD.txt`
- Retention: 30 days
- Max size: 10MB per file

### Regular Maintenance Tasks

1. **Weekly**:
   - Review error logs
   - Check subscription status
   - Monitor database size

2. **Monthly**:
   - Review and archive old logs
   - Check for security updates
   - Review user feedback

3. **Quarterly**:
   - Update NuGet packages
   - Review and optimize database
   - Performance testing

## Troubleshooting

### Application Won't Start
1. Check `logs/stdout` file for errors
2. Verify connection string is correct
3. Ensure database is accessible
4. Check IIS application pool is running

### Email Not Sending
1. Verify SendGrid API key is correct
2. Check SendGrid account is not suspended
3. Review logs for email errors
4. Verify sender email is verified in SendGrid

### Payment Not Working
1. Ensure Stripe keys are for correct environment (test/live)
2. Verify webhook endpoint is accessible
3. Check Stripe Dashboard for webhook delivery status
4. Review logs for Stripe errors

### Database Connection Issues
1. Verify connection string format
2. Check database user has proper permissions
3. Ensure firewall allows database connections
4. Test connection from SmartASP.NET control panel

## Security Checklist

- [ ] SSL certificate installed and working
- [ ] All secrets moved to production configuration
- [ ] SendGrid API key is secure
- [ ] Stripe keys are for production (not test)
- [ ] Admin email is correct
- [ ] Database backups are configured
- [ ] Rate limiting is active
- [ ] Security headers are being sent
- [ ] CORS is properly configured
- [ ] Antiforgery tokens are working

## Performance Optimization

### Recommended Settings
1. Enable output caching in IIS
2. Configure compression (already enabled in code)
3. Use CDN for static assets (optional)
4. Monitor memory usage
5. Set up database indexing (already configured)

### Database Optimization
```sql
-- Run these queries periodically to optimize database
UPDATE STATISTICS;
DBCC CHECKDB;
```

## Backup Strategy

### Database Backups
1. Configure automatic backups in SmartASP.NET control panel
2. Recommended schedule: Daily
3. Retention: 30 days minimum
4. Test restore process monthly

### Application Backups
1. Keep a copy of published files
2. Version control all source code
3. Document all configuration changes

## Support and Resources

- SmartASP.NET Support: https://www.smarterasp.net/support
- Stripe Documentation: https://stripe.com/docs
- SendGrid Documentation: https://docs.sendgrid.com
- ASP.NET Core Deployment: https://docs.microsoft.com/aspnet/core/host-and-deploy

## Post-Deployment Checklist

- [ ] Application is accessible via HTTPS
- [ ] Health checks return "Healthy"
- [ ] User registration works
- [ ] Email verification works
- [ ] Quiz functionality works
- [ ] Leaderboard displays correctly
- [ ] Payment flow works (test mode)
- [ ] Stripe webhooks are being received
- [ ] Admin panel is accessible
- [ ] Logs are being written
- [ ] Database migrations completed
- [ ] SSL certificate is valid
- [ ] All secrets are configured
- [ ] Backups are configured

## Going Live

Before switching Stripe to live mode:

1. Test all functionality thoroughly in test mode
2. Verify webhook handling works correctly
3. Test subscription lifecycle (create, update, cancel)
4. Review all pricing and product descriptions
5. Update Stripe keys to live mode
6. Test with real payment (small amount)
7. Monitor closely for first 24 hours

## Congratulations!

Your Blazor Quiz App is now live on SmartASP.NET! 🎉

Monitor the application closely for the first few days and be ready to respond to any issues quickly.
