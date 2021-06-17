# nxfw
Tool to download Nintendo Switch firmware updates directly from CDN

### Preparations
Dump your `prod.keys` and PRODINFO, extract DeviceID and device certificate from your PRODINFO dump.

### Usage
```
$ nxfw /path/to/prod.keys /path/to/cert.pfx /path/to/systemVersion /path/to/output/directory DeviceId EnvironmentId
Example:
$ nxfw prod.keys cert.pfx currentVersion out 0123456789ABCDEF lp1
```

### Disclaimer
This tool is provided **AS IS** with absolutely **NO WARRANTY**.  
I am not responsible for anything if you get your device certificate super-banned.
Only use this with certificates that are already banned from regular CDN access.
