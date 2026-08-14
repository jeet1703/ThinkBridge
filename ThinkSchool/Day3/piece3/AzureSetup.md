# Azure Container Apps Fundamentals Setup

This document records the creation of the Azure Container Apps environment.

## 1. Resource Group Creation Command
```powershell
az group create -n thinkschool-rg -l centralindia
```

## 2. Container Apps Environment Creation Command
```powershell
az containerapp env create -n thinkschool-env -g thinkschool-rg -l centralindia
```

---

## 3. Environment Details (`az containerapp env show`)

```json
{
  "id": "/subscriptions/8a87d84d-ff03-4d3e-bd9a-6951441f9028/resourceGroups/thinkschool-rg/providers/Microsoft.App/managedEnvironments/thinkschool-env",
  "location": "Central India",
  "name": "thinkschool-env",
  "properties": {
    "appInsightsConfiguration": null,
    "appLogsConfiguration": {
      "destination": "log-analytics",
      "logAnalyticsConfiguration": {
        "customerId": "16496f77-0dd4-4586-8ab1-f74701508b83",
        "sharedKey": null
      }
    },
    "customDomainConfiguration": {
      "certificateKeyVaultProperties": null,
      "certificatePassword": null,
      "certificateValue": null,
      "customDomainVerificationId": "FA4557A79BC22077D8EED3760592A23C9BF852D1ED66F147764E64F0014B8E31",
      "dnsSuffix": null,
      "expirationDate": null,
      "subjectName": null,
      "thumbprint": null
    },
    "daprAIConnectionString": null,
    "daprAIInstrumentationKey": null,
    "daprConfiguration": {
      "version": "1.16.4-msft.11"
    },
    "defaultDomain": "ashystone-87f8450e.centralindia.azurecontainerapps.io",
    "eventStreamEndpoint": "https://centralindia.azurecontainerapps.dev/subscriptions/8a87d84d-ff03-4d3e-bd9a-6951441f9028/resourceGroups/thinkschool-rg/managedEnvironments/thinkschool-env/eventstream",
    "infrastructureResourceGroup": null,
    "ingressConfiguration": null,
    "kedaConfiguration": {
      "version": "2.18.1"
    },
    "openTelemetryConfiguration": null,
    "peerAuthentication": {
      "mtls": {
        "enabled": false
      }
    },
    "peerTrafficConfiguration": {
      "encryption": {
        "enabled": false
      }
    },
    "provisioningState": "Succeeded",
    "publicNetworkAccess": "Enabled",
    "staticIp": "135.13.179.196",
    "vnetConfiguration": null,
    "workloadProfiles": [
      {
        "enableFips": false,
        "name": "Consumption",
        "workloadProfileType": "Consumption"
      }
    ],
    "zoneRedundant": false
  },
  "resourceGroup": "thinkschool-rg",
  "systemData": {
    "createdAt": "2026-08-14T10:22:01.0296036",
    "createdBy": "riya.gupta25@s.amity.edu",
    "createdByType": "User",
    "lastModifiedAt": "2026-08-14T10:22:01.0296036",
    "lastModifiedBy": "riya.gupta25@s.amity.edu",
    "lastModifiedByType": "User"
  },
  "type": "Microsoft.App/managedEnvironments"
}
```
