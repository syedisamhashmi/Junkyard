# Junkyard
Check junkyard inventory automatically.

## To set up:
1.) Run `dotnet user-secrets init` in the project directory 

(I suggest you remove `<UserSecretsId>661bb1cf-857e-4f4e-9d31-df561db8d894</UserSecretsId>` from the csproj beforehand)

2.) Add keys for "Name", "Email", and "AccessKey" on the "OutgoingMailBox" section by running:

`dotnet user-secrets set "OutgoingMailBox:Name" "{{MAILBOX_NAME_HERE}}"`

`dotnet user-secrets set "OutgoingMailBox:Email" "{{MAILBOX_EMAIL_HERE}}"`

`dotnet user-secrets set "OutgoingMailBox:AccessKey" "{{MAILBOX_ACCESSKEY_HERE}}"`

(If this doesn't work, go to `~/.microsoft/usersecrets/GENERATED_GUID_MATCHING_CSPROJ/secrets.json` and edit it to match the `appsettings.json` there manually.)