﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Messaging;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backplane;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server.Backplane;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Messaging.Server.HostedServices
{
    public sealed class HandlePartitionsChanged : IHostedService, ISubscriber<PartitionsChangedEventData>
    {
        private readonly IBackplaneComponents _backplaneComponents;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IClientContainer _clientContainer;
        private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
        private readonly Guid _partitionsChangedToken;

        public HandlePartitionsChanged(
            IBackplaneComponents backplaneComponents,
            IPartitionUtility partitionUtility,
            IClientContainer clientContainer,
            IEvent<PartitionsChangedEventData> partitionsChanged)
        {
            _backplaneComponents = backplaneComponents;
            _partitionUtility = partitionUtility;
            _clientContainer = clientContainer;
            _partitionsChanged = partitionsChanged;

            _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            return Task.CompletedTask;
        }

        public ValueTask Handle(PartitionsChangedEventData eventData)
        {
            int partitionCount = eventData.PartitionCount;
            PartitionRange partitions = eventData.Partitions;

            DisconnectClients(partitionCount, partitions);
            _backplaneComponents.ConfigurePartitioning(partitionCount, partitions);

            return ValueTask.CompletedTask;
        }

        private void DisconnectClients(int partitionCount, PartitionRange partitions)
        {
            ListenNotification notification = new() {Type = MessageType.Disconnect};

            foreach (KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>> pair in _clientContainer.EnumerateAllClients())
            {
                long userID = pair.Key;
                IEnumerable<IStreamer<ListenNotification>> clients = pair.Value;

                int userPartition = _partitionUtility.ChoosePartition(userID, partitionCount);
                if (!partitions.Contains(userPartition))
                {
                    foreach (IStreamer<ListenNotification> client in clients)
                    {
                        client.EnqueueMessage(notification);
                    }
                }
            }
        }
    }
}
