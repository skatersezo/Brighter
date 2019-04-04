using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Paramore.Brighter
{
    public class GenericJsonMessageMapper<T> : IAmAMessageMapper<T> where T : class, IRequest
    {
        private readonly IRequestContext _requestContext;
        private readonly Func<T, string> _routingAction;
        private readonly RoutingKey _routingKey;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public GenericJsonMessageMapper(IRequestContext requestContext, RoutingKey routingKey = null,  Func<T, string> routingKeyFunc = null)
        {
            _requestContext = requestContext;
            _routingKey = routingKey;
            _routingAction = routingKeyFunc;
        }

        public Message MapToMessage(T request)
        {
            MessageType messageType;
            if (request is Command)
                messageType = MessageType.MT_COMMAND;
            else if (request is Event)
                messageType = MessageType.MT_EVENT;
            else
            {
                throw new ArgumentException("This message mapper can only map Commands and Events", nameof(request));
            }
            
            var topic = _routingAction?.Invoke(request) ?? _routingKey ?? request.GetType().Name;

            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream, new UTF8Encoding(false)))
                using (var jsonTextWriter = new JsonTextWriter(streamWriter))
                {
                    _serializer.Serialize(jsonTextWriter, request, typeof(T));
                }

                return new Message(new MessageHeader(request.Id, topic, messageType, _requestContext.Header.CorrelationId, contentType: "application/json"), new MessageBody(memoryStream.ToArray(), "JSON"));
            }
        }

        public T MapToRequest(Message message)
        {
            _requestContext.Header.CorrelationId = message.Header.CorrelationId;
            
            using (var memoryStream = new MemoryStream(message.Body.Bytes))
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
            using (JsonReader reader = new JsonTextReader(streamReader))
            {
                return _serializer.Deserialize<T>(reader);
            }
        }
    }
}
