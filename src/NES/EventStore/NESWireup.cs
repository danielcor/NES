using System.Collections.Generic;
using System.Collections.ObjectModel;
using EventStore;
using EventStore.Dispatcher;
using EventStore.Logging;
using EventStore.Persistence;
using EventStore.Serialization;

namespace NES.EventStore
{
    public class NESWireup : Wireup
    {
        private static readonly ILog _logger = LogFactory.BuildLogger(typeof(NESWireup));

        public NESWireup(Wireup wireup)
            : base(wireup)
        {
            _logger.Debug("Configuring serializer to cope with payloads that contain messages as interfaces.");
            _logger.Debug("Wrapping serializer of type '" + Container.Resolve<ISerialize>().GetType() + "' in '" + typeof(Serializer) + "'");

            Container.Register<ISerialize>(new Serializer(Container.Resolve<ISerialize>(), () => DI.Current.Resolve<IEventSerializer>()));
            
            _logger.Debug("Configuring the store to dispatch messages synchronously.");
            _logger.Debug("Registering dispatcher of type '" + typeof(MessageDispatcher) + "'.");

            Container.Register<IScheduleDispatches>(c => new SynchronousDispatchScheduler(new MessageDispatcher(() => DI.Current.Resolve<IEventPublisher>()), c.Resolve<IPersistStreams>()));
            
            DI.Current.Register<IEventStore, IStoreEvents>(eventStore => new EventStoreAdapter(eventStore));
            DI.Current.Register(() => Container.Resolve<IStoreEvents>());
        }

        public override IStoreEvents Build()
        {
            _logger.Debug("Configuring the store to upconvert events when fetched.");

            var pipelineHooks = Container.Resolve<ICollection<IPipelineHook>>();
            var eventConverterPipelineHook = new EventConverterPipelineHook(() => DI.Current.Resolve<IEventConversionRunner>());

            if (pipelineHooks == null)
            {
                Container.Register((pipelineHooks = new Collection<IPipelineHook>()));
            }

            pipelineHooks.Add(eventConverterPipelineHook);

            return base.Build();
        }
    }
}