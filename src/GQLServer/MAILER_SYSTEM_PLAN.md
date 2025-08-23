# Email Queue System Implementation Plan

## Architecture Overview

A flexible, multi-tenant email system where all emails are queued to a database table and processed by a background job using Hangfire.

## Database Schema

### 1. `email_outbox` Table
```sql
- id (guid, PK)
- to_email (string, required)
- cc_email (string, nullable)
- bcc_email (string, nullable)
- from_email (string, required)
- from_name (string, nullable)
- subject (string, required)
- html_body (text, nullable)
- text_body (text, nullable)
- template_id (guid, FK, nullable)
- template_data (jsonb, nullable)
- sender_type (enum: System, User)
- sender_id (string, nullable) - User ID if sender_type is User
- mail_driver (enum: SystemSmtp, UserSmtp, SendGrid, Mailgun, AwsSes)
- priority (int, default: 5) - 1-10, 1 being highest
- status (enum: Pending, Processing, Sent, Failed, Cancelled)
- attempts (int, default: 0)
- max_attempts (int, default: 3)
- last_attempt_at (datetime, nullable)
- sent_at (datetime, nullable)
- failed_reason (text, nullable)
- scheduled_for (datetime, nullable) - For delayed sending
- metadata (jsonb, nullable) - Additional tracking data
- created_at (datetime)
- updated_at (datetime)
```

### 2. `email_templates` Table
```sql
- id (guid, PK)
- name (string, unique, required) - e.g., "welcome_email", "password_reset"
- subject_template (string, required) - With placeholders like {{userName}}
- html_body_template (text, nullable)
- text_body_template (text, nullable)
- default_from_email (string, nullable)
- default_from_name (string, nullable)
- is_active (bool, default: true)
- created_by (string, nullable)
- created_at (datetime)
- updated_at (datetime)
```

### 3. `user_mail_settings` Table
```sql
- id (guid, PK)
- user_id (string, unique, required)
- mail_driver (enum: Smtp, SendGrid, Mailgun, AwsSes)
- is_enabled (bool, default: true)
- 
- # SMTP Settings (encrypted)
- smtp_host (string, nullable)
- smtp_port (int, nullable)
- smtp_username (string, nullable)
- smtp_password (string, encrypted, nullable)
- smtp_encryption (enum: None, SSL, TLS, nullable)
- smtp_from_email (string, nullable)
- smtp_from_name (string, nullable)
- 
- # API Settings (encrypted)
- api_key (string, encrypted, nullable)
- api_secret (string, encrypted, nullable)
- api_domain (string, nullable) - For Mailgun
- api_region (string, nullable) - For AWS SES
- 
- daily_limit (int, nullable) - Rate limiting
- sent_today (int, default: 0)
- last_sent_at (datetime, nullable)
- 
- created_at (datetime)
- updated_at (datetime)
```

### 4. `email_logs` Table (Optional - for audit)
```sql
- id (guid, PK)
- email_outbox_id (guid, FK)
- action (enum: Queued, Processing, Sent, Failed, Retried, Cancelled)
- details (text, nullable)
- created_at (datetime)
```

## C# Implementation Structure

### Services

```csharp
// Core Interfaces
IMailerService          // Main service for queuing emails
IMailProcessor          // Background job that processes queue
IMailDriver             // Interface for mail drivers
IMailTemplateService    // Template management
IUserMailSettingsService // User mail settings management

// Mail Driver Implementations
SystemSmtpMailDriver    // Uses app settings SMTP
UserSmtpMailDriver      // Uses user's SMTP settings
SendGridMailDriver      // Future: SendGrid API
MailgunMailDriver       // Future: Mailgun API
AwsSesMailDriver        // Future: AWS SES
```

### Entities

```csharp
EmailOutbox
EmailTemplate
UserMailSettings
EmailLog
```

### DTOs/ViewModels

```csharp
SendEmailRequest
EmailTemplateData
MailSettingsInput
EmailStatusResponse
```

### GraphQL Integration

```csharp
// Mutations
SendEmail
SendTemplatedEmail
UpdateMailSettings
CancelScheduledEmail
RetryFailedEmail

// Queries
GetEmailStatus
GetEmailHistory
GetMailSettings
GetEmailTemplates

// Subscriptions
OnEmailStatusChanged
```

## Hangfire Setup

### Installation
```bash
dotnet add package Hangfire.Core
dotnet add package Hangfire.PostgreSql
dotnet add package Hangfire.AspNetCore
```

### Configuration in Program.cs
```csharp
// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(connectionString));

// Add Hangfire server
builder.Services.AddHangfireServer();

// Map dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Register recurring job
RecurringJob.AddOrUpdate<IMailProcessor>(
    "process-email-queue",
    processor => processor.ProcessPendingEmails(),
    "*/1 * * * *"); // Every minute
```

## Implementation Steps

### Phase 1: Core Infrastructure
1. Create database migrations for all tables
2. Create entity models with Entity Framework mappings
3. Install and configure Hangfire
4. Create basic IMailerService interface and implementation
5. Create IMailDriver interface with SystemSmtpMailDriver

### Phase 2: Queue Processing
1. Implement IMailProcessor background job
2. Add retry logic with exponential backoff
3. Implement status updates and logging
4. Add Hangfire dashboard authentication

### Phase 3: User Mail Settings
1. Create UserMailSettings CRUD operations
2. Implement UserSmtpMailDriver
3. Add encryption for sensitive credentials
4. Implement driver selection logic in processor

### Phase 4: Templates
1. Create template management system
2. Add template variable replacement (Handlebars/Liquid style)
3. Create common templates (welcome, password reset, etc.)
4. Add template preview functionality

### Phase 5: GraphQL Integration
1. Add mutations for sending emails
2. Add queries for email status and history
3. Add subscriptions for real-time status updates
4. Add GraphQL authorization for mail operations

### Phase 6: Advanced Features
1. Add rate limiting per user
2. Add email attachments support
3. Add bounce/complaint handling
4. Add webhook support for email events
5. Implement additional mail drivers (SendGrid, Mailgun, etc.)

## Security Considerations

1. **Encryption**: All credentials stored encrypted using .NET Data Protection API
2. **Authorization**: Users can only see/manage their own mail settings and sent emails
3. **Rate Limiting**: Prevent email bombing with daily/hourly limits
4. **Validation**: Strict email validation and content sanitization
5. **Audit Trail**: Complete logging of all email operations

## Seq Integration for Centralized Logging

### Installation
```bash
# Run Seq with Docker
docker run -d --name seq \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  -p 5342:5342 \
  -v /path/to/seq/data:/data \
  datalust/seq:latest

# Add Seq sink to project
dotnet add package Serilog.Sinks.Seq
```

### Configuration
```csharp
// In LogService.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Seq("http://localhost:5341", apiKey: Environment.GetEnvironmentVariable("SEQ_API_KEY"))
    .WriteTo.File(...)
    .WriteTo.Spectre(...)
    .CreateLogger();
```

### Benefits for Email System
1. **Track email processing**: Search logs by email ID, user, status
2. **Monitor Hangfire jobs**: See job execution times, failures, retries
3. **Debug mail drivers**: Trace SMTP connections, API calls
4. **Performance metrics**: Query for slow email sends, queue depth
5. **Alerts**: Set up alerts for failed emails, queue backlogs

### Seq Dashboards for Email System
- Email throughput (emails/minute)
- Failed email trends
- Average processing time
- Queue depth over time
- Per-user email statistics

## Environment Configuration

```env
# System SMTP (appsettings.json or env vars)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=system@example.com
SMTP_PASSWORD=encrypted_password
SMTP_FROM_EMAIL=noreply@example.com
SMTP_FROM_NAME=GQLServer

# Hangfire
HANGFIRE_DASHBOARD_PATH=/hangfire
HANGFIRE_WORKER_COUNT=2
HANGFIRE_POLL_INTERVAL=15

# Seq
SEQ_URL=http://localhost:5341
SEQ_API_KEY=your_api_key_here
```

## Testing Strategy

1. **Unit Tests**: Service logic, driver implementations
2. **Integration Tests**: Database operations, queue processing
3. **Mock Driver**: TestMailDriver that logs instead of sending
4. **Load Testing**: Queue performance with high volume

## Monitoring

1. **Hangfire Dashboard** at `/hangfire` - Job execution and queue status
2. **Seq Dashboard** at `http://localhost:5341` - Centralized structured logs
3. **Failed email alerts** - Real-time notifications via Seq
4. **Queue depth monitoring** - Track backlog and processing rate
5. **Delivery rate metrics** - Success/failure ratios
6. **Bounce/complaint tracking** - Email reputation management

### Key Metrics to Track in Seq
- `EmailQueued` - When email enters outbox
- `EmailProcessing` - When Hangfire picks up email
- `EmailSent` - Successful delivery
- `EmailFailed` - Failed with reason
- `EmailRetry` - Retry attempts
- `QueueDepth` - Current pending emails
- `ProcessingTime` - Time to send each email

## Benefits of This Architecture

1. **Resilient**: Failures don't lose emails, automatic retries
2. **Scalable**: Can add more Hangfire workers as needed
3. **Flexible**: Easy to add new mail providers
4. **Multi-tenant**: Each user can have own mail configuration
5. **Auditable**: Complete history of all email operations
6. **Testable**: Can test without sending actual emails
7. **Observable**: Dashboard and metrics for monitoring

## Esquio Feature Flag System Integration

### Installation
```bash
dotnet add package Esquio.AspNetCore
dotnet add package Esquio.EntityFrameworkCore.Store
dotnet add package Esquio.AspNetCore.ApplicationInsights  # Optional: for telemetry
dotnet add package Esquio.UI                              # Admin UI for feature management
```

### Configuration in Program.cs
```csharp
// Add Esquio services
builder.Services.AddEsquio(options =>
{
    options.ConfigureDefaultProductName("GQLServer");
    options.ConfigureOnErrorBehavior(OnErrorBehavior.SetDisabled);
})
.AddEntityFrameworkCoreStore(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureDbContext(builder => 
        builder.UseSnakeCaseNamingConvention());
})
.AddAspNetCoreDefaultServices()
.AddApplicationInsightProcessor(); // Optional

// Add Esquio UI (admin panel)
builder.Services.AddEsquioUI();

// In app configuration
app.UseEsquio();
app.UseEsquioUI(); // Available at /esquio-ui
```

### Feature Flags for Email System

```csharp
// Define feature flags
public static class EmailFeatures
{
    public const string EmailQueueSystem = "EmailQueueSystem";
    public const string TemplateEngine = "TemplateEngine";
    public const string UserSmtpSettings = "UserSmtpSettings";
    public const string SendGridIntegration = "SendGridIntegration";
    public const string MailgunIntegration = "MailgunIntegration";
    public const string EmailAnalytics = "EmailAnalytics";
    public const string RateLimiting = "RateLimiting";
    public const string EmailScheduling = "EmailScheduling";
    public const string BounceHandling = "BounceHandling";
    public const string EmailWebhooks = "EmailWebhooks";
}

// Usage in code
public class MailerService : IMailerService
{
    private readonly IFeatureService _featureService;
    
    public async Task<EmailQueuedResponse> QueueEmailAsync(SendEmailRequest request)
    {
        // Check if email queue system is enabled
        if (!await _featureService.IsEnabledAsync(EmailFeatures.EmailQueueSystem))
        {
            // Fallback to direct sending
            return await SendDirectAsync(request);
        }
        
        // Check if rate limiting is enabled
        if (await _featureService.IsEnabledAsync(EmailFeatures.RateLimiting))
        {
            await CheckRateLimitsAsync(request.UserId);
        }
        
        // Queue the email
        return await QueueToOutboxAsync(request);
    }
}
```

### Feature Toggle Strategies

```yaml
Deployment Rings:
  - Development: All features enabled
  - Staging: Selected features for testing
  - Production: Gradual rollout with toggles

User-Based Toggles:
  - Premium users: Advanced features (SendGrid, Mailgun)
  - Free users: Basic SMTP only
  - Beta testers: Early access features

Percentage Rollout:
  - 10% users: New template engine
  - 25% users: Email analytics
  - 100% users: Core email functionality

Time-Based Toggles:
  - Business hours: Full features
  - Off-hours: Reduced features for maintenance

Environment Toggles:
  - Dev: All experimental features
  - Test: Integration testing features
  - Prod: Stable features only
```

### Esquio UI Features

The Esquio UI provides:
1. **Feature Management Dashboard** at `/esquio-ui`
2. **Real-time toggle control** without deployment
3. **Audit logging** of all feature changes
4. **User/group targeting** for specific rollouts
5. **A/B testing capabilities**
6. **Metrics and analytics** for feature adoption
7. **REST API** for programmatic control

### Integration with Email System

```csharp
// GraphQL Mutation with feature flags
[MutationType]
public class EmailMutations
{
    public async Task<EmailQueuedResponse> SendEmail(
        SendEmailRequest request,
        [Service] IMailerService mailerService,
        [Service] IFeatureService featureService)
    {
        // Check if entire email system is enabled
        if (!await featureService.IsEnabledAsync(EmailFeatures.EmailQueueSystem))
        {
            throw new GraphQLException("Email system is currently disabled");
        }
        
        // Check for premium features
        if (request.UseSendGrid && 
            !await featureService.IsEnabledAsync(EmailFeatures.SendGridIntegration))
        {
            throw new GraphQLException("SendGrid integration is not available");
        }
        
        return await mailerService.QueueEmailAsync(request);
    }
}
```

### Database Schema for Esquio

Esquio creates its own tables:
- `Features`: Feature definitions
- `Toggles`: Toggle configurations
- `Parameters`: Toggle parameters
- `Products`: Product definitions
- `FeatureStates`: Current feature states
- `History`: Audit log of changes

### Benefits of Esquio Integration

1. **Risk Mitigation**: Roll back features instantly without deployment
2. **Progressive Delivery**: Gradual rollout to user segments
3. **A/B Testing**: Test email delivery methods and templates
4. **Kill Switch**: Disable problematic features immediately
5. **Configuration Management**: Centralized feature configuration
6. **Audit Trail**: Complete history of feature changes
7. **Performance**: Minimal overhead with caching

## Next Session Tasks

When we continue, we'll start with:
1. ✅ Creating the database migrations
2. ✅ Setting up Hangfire (partially done, need to integrate with Program.cs)
3. Implementing the basic MailerService
4. Creating the email processor job
5. Adding GraphQL mutations for sending emails
6. Setting up Esquio for feature flag management