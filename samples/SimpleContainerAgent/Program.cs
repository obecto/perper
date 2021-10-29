using System;

using Perper.Application;
using Perper.Model;
using Perper.Protocol.Cache.Notifications;

var agent = "simple-container-agent";

await PerperStartup.EnterServicesContext(agent, async () =>
{
    var (notificationKey, callNotification) = await AsyncLocals.NotificationService.GetCallTriggerNotifications(Context.StartupFunctionName).ReadAsync().ConfigureAwait(false);

    await AsyncLocals.EnterContext(callNotification.Instance, callNotification.Call, async () =>
    {
        var parameters = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Execution).ConfigureAwait(false);
        await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
        await AsyncLocals.NotificationService.ConsumeNotification(notificationKey).ConfigureAwait(false);
    }).ConfigureAwait(false);

    var id = Guid.NewGuid();

    await foreach (var (notification2Key, notification) in AsyncLocals.NotificationService.GetCallTriggerNotifications("Test").ReadAllAsync())
    {
        if (notification is CallTriggerNotification ct)
        {
            await AsyncLocals.CacheService.CallWriteResult(ct.Call, id).ConfigureAwait(false);
            await AsyncLocals.NotificationService.ConsumeNotification(notification2Key).ConfigureAwait(false);
        }
    }

}, true).ConfigureAwait(false);