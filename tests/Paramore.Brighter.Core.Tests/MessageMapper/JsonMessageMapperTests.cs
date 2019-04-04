using System;
using System.Text;
using Xunit;

namespace Paramore.Brighter.Tests.MessageMapper
{
    public class JsonMessageMapperTests
    {
        [Fact]
        public void When_mapping_an_event_to_a_message_as_json()
        {
            var requestContext = new RequestContext();

            var mapper = new GenericJsonMessageMapper<TestedEvent>(requestContext);

            DateTime dateTime = DateTime.UtcNow;
            TestedEvent testedEvent = new TestedEvent
            {
                Message = "This is a message",
                Number = 999,
                DateNow = dateTime
            };

            requestContext.Header.CorrelationId = Guid.NewGuid();

            var message = mapper.MapToMessage(testedEvent);

            // Message checks
            Assert.Equal(testedEvent.Id, message.Id);

           // Header checks
            Assert.Equal("application/json", message.Header.ContentType);
            Assert.Equal("TestedEvent", message.Header.Topic);
            Assert.Equal(MessageType.MT_EVENT , message.Header.MessageType);
            Assert.Equal( requestContext.Header.CorrelationId , message.Header.CorrelationId);

            // Body checks
            Assert.Equal("JSON", message.Body.BodyType);
            Assert.Equal($"{{\"Message\":\"{testedEvent.Message}\",\"Number\":{testedEvent.Number},\"DateNow\":\"{dateTime:O}\",\"Id\":\"{testedEvent.Id}\"}}", message.Body.Value);
        }


        [Fact]
        public void When_mapping_a_command_to_a_message_as_json()
        {
            var requestContext = new RequestContext();

            var mapper = new GenericJsonMessageMapper<TestCommand>(requestContext);

            DateTime dateTime = DateTime.UtcNow;
            TestCommand testCommand = new TestCommand
            {
                Message = "This is a message",
                Number = 999,
                DateNow = dateTime
            };

            requestContext.Header.CorrelationId = Guid.NewGuid();

            var message = mapper.MapToMessage(testCommand);

            // Message checks
            Assert.Equal(testCommand.Id, message.Id);

            // Header checks
            Assert.Equal("application/json", message.Header.ContentType);
            Assert.Equal("TestCommand", message.Header.Topic);
            Assert.Equal(MessageType.MT_COMMAND , message.Header.MessageType);
            Assert.Equal( requestContext.Header.CorrelationId , message.Header.CorrelationId);

            // Body checks
            Assert.Equal("JSON", message.Body.BodyType);
            Assert.Equal($"{{\"Message\":\"{testCommand.Message}\",\"Number\":{testCommand.Number},\"DateNow\":\"{dateTime:O}\",\"Id\":\"{testCommand.Id}\"}}", message.Body.Value);
        }

        [Fact]
        public void When_mapping_with_custom_routing_key_to_a_message()
        {
            var mapper = new GenericJsonMessageMapper<TestCommand>(new RequestContext(), new RoutingKey("MyTestRoute"));

            var testCommand = new TestCommand();

            var message = mapper.MapToMessage(testCommand);

            Assert.Equal("MyTestRoute", message.Header.Topic);
        }

        [Fact]
        public void When_mapping_with_custom_routing_key_to_a_message2()
        {
            var mapper = new GenericJsonMessageMapper<TestCommand>(new RequestContext(), routingKeyFunc: request =>
            {
                string topic = "TestPreAmble.";

                string name = request.GetType().Name;

                if (name.EndsWith("Command", StringComparison.InvariantCultureIgnoreCase))
                    topic = topic + name.Replace("Command", "", StringComparison.InvariantCultureIgnoreCase);
                else if (name.EndsWith("Event", StringComparison.InvariantCultureIgnoreCase))
                    topic = topic + name.Replace("Event", "", StringComparison.InvariantCultureIgnoreCase);
                else
                {
                    topic = topic + name;
                }

                return topic;
            } );

            var testCommand = new TestCommand();

            var message = mapper.MapToMessage(testCommand);

            Assert.Equal("TestPreAmble.Test", message.Header.Topic);
        }


    }

    public class TestCommand : Command
    {
        public TestCommand() : base(Guid.NewGuid())
        {
        }

        public string Message { get; set; }
        public int Number { get; set; }
        public DateTime DateNow { get; set; }
    }

    public class TestedEvent : Event
    {
        public TestedEvent() : base(Guid.NewGuid())
        {
        }

        public string Message { get; set; }
        public int Number { get; set; }
        public DateTime DateNow { get; set; }
    }
}
