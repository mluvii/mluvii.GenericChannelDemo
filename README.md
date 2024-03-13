# Demo chat application using mluvii Generic Channel

## Run on local machine

### Start redis

```
docker run -d -p 6379:6379 --name redis redis
```

### Run the solution in Rider

Use run configuration `mluvii.GenericChannelDemo.Web/Dockerfile`

### Set up generic channel in mluvii

Generate a random subscription_uuid.

Insert the data into the database:

| column                   | value                                             | 
|--------------------------|---------------------------------------------------|
| generic_channel_subscription_id | `subscription_uuid` |
| auth_user_name           | mluvii                                            |
| auth_password            | a                                                 |
| webhook_registration_url | http://127.0.0.1:8123/api/GenericChannel/Webhook  |
| webhook_password         | test                                              |
| activity_sending_url     | http://127.0.0.1:8123/api/GenericChannel/Activity |

Log in to mluvii as `admin@mluvii.com` and open settings.

Open developer console and run:
```
settingsApi.genericChannel.registerWebhook(1, subscription_uuid);
```
