using System;

using Perper.Application;
using Perper.Extensions;

var agent = "container-sample";

AsyncLocals.SetConnection(await PerperStartup.EstablishConnection(agent, true).ConfigureAwait(false));

var (notificationKey, callNotification) = await AsyncLocals.NotificationService.GetCallTriggerNotifications(PerperContext.StartupFunctionName).ReadAsync().ConfigureAwait(false);

await AsyncLocals.EnterContext(callNotification.Instance, callNotification.Call, async () =>
{
    var parameters = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Execution).ConfigureAwait(false);
    await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
    await AsyncLocals.NotificationService.ConsumeNotification(notificationKey).ConfigureAwait(false);
}).ConfigureAwait(false);

var id = Guid.NewGuid();

await foreach (var (notification2Key, notification) in AsyncLocals.NotificationService.GetCallTriggerNotifications("Test").ReadAllAsync())
{
    await AsyncLocals.CacheService.CallWriteResult(notification.Call, new object[] { id }).ConfigureAwait(false);
    await AsyncLocals.NotificationService.ConsumeNotification(notification2Key).ConfigureAwait(false);
}

await AsyncLocals.NotificationService.StopAsync().ConfigureAwait(false);