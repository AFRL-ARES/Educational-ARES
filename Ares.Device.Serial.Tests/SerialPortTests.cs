using Ares.Device.Serial.Commands;
using Ares.Device.Serial.Simulation;
using System.IO.Ports;
using System.Reactive.Linq;
using System.Text;

namespace Ares.Device.Serial.Tests;

internal class SerialPortTests
{
  [Test]
  [CancelAfter(5000)]
  public async Task AresSerialPort_Returns_Good_Response_From_Simple_Request()
  {
    const string stringToTest = "<-Oh Hello->";
    var port = new TestConnection(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var response = await port.Send(new SomeCommandWithResponse(stringToTest));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    Assert.That(response, Is.Not.Null);
    Assert.That(response.Response, Is.EqualTo(stringToTest));
    // var currentBuffer = await port.DataBufferState.FirstAsync();
    // Assert.That(currentBuffer, Is.Empty);
  }

  [Test]
  [CancelAfter(5000)]
  public async Task AresSerialPort_Returns_Good_Response_From_Multiple_Data_Adds()
  {
    const string stringToTest = "<-Oh Hello->";
    var port = new TestPort2(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var response = await port.Send(new SomeCommandWithResponse(stringToTest));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    Assert.That(response, Is.Not.Null);
    Assert.That(response.Response, Is.EqualTo(stringToTest));
    // var currentBuffer = await port.DataBufferState.FirstAsync();
    // Assert.That(currentBuffer, Is.Empty);
  }

  [Test]
  [CancelAfter(5000)]
  public async Task AresSerialPort_Returns_Good_Response_From_Multiple_Data_And_Commands()
  {
    const string stringToTest = "<-Oh Hello->";
    const string stringToTest2 = "<-Noice->";
    const string stringToTest3 = "<-This Is A Test->";
    var port = new TestPort2(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var test1 = await port.Send(new SomeCommandWithResponse(stringToTest));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    var test2 = await port.Send(new SomeCommandWithResponse(stringToTest2));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    var test3 = await port.Send(new SomeCommandWithResponse(stringToTest3));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    Assert.Multiple(() =>
    {
      Assert.That(test1, Is.Not.Null);
      Assert.That(test2, Is.Not.Null);
      Assert.That(test3, Is.Not.Null);
    });

    Assert.Multiple(() =>
    {
      Assert.That(test1.Response, Is.EqualTo(stringToTest));
      Assert.That(test2.Response, Is.EqualTo(stringToTest2));
      Assert.That(test3.Response, Is.EqualTo(stringToTest3));
    });

    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
  }

  [Test]
  [CancelAfter(5000)]
  public async Task AresSerialPort_Returns_Good_Response_From_Multiple_Types_Of_Commands()
  {
    const string stringToTest = "<-Oh Hello->";
    const string stringToTest2 = "!-Noice-!";
    const string stringToTest3 = "<-This Is A Test->";
    const string stringToTest4 = "!-More Tests-!";
    var port = new TestPort2(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var test1 = await port.Send(new SomeCommandWithResponse(stringToTest));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    var test2 = await port.Send(new SomeCommandWithResponse2(stringToTest2));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    var test3 = await port.Send(new SomeCommandWithResponse(stringToTest3));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    var test4 = await port.Send(new SomeCommandWithResponse2(stringToTest4));
    // Assert.That(await port.DataBufferState.FirstAsync(), Is.Empty);
    Assert.Multiple(() =>
    {
      Assert.That(test1, Is.Not.Null);
      Assert.That(test2, Is.Not.Null);
      Assert.That(test3, Is.Not.Null);
      Assert.That(test4, Is.Not.Null);
    });

    Assert.Multiple(() =>
    {
      Assert.That(test1.Response, Is.EqualTo(stringToTest));
      Assert.That(test2.OtherResponse, Is.EqualTo(stringToTest2));
      Assert.That(test3.Response, Is.EqualTo(stringToTest3));
      Assert.That(test4.OtherResponse, Is.EqualTo(stringToTest4));
    });

    // var currentBuffer = await port.DataBufferState.FirstAsync();
    // Assert.That(currentBuffer, Is.Empty);
  }

  [Test]
  [CancelAfter(5000)]
  [Ignore("Might not be a good idea to send asynchronously anyways.")]
  public async Task AresSerialPort_Returns_Good_Response_From_Multiple_Types_Of_Commands_Asynchronously()
  {
    const string stringToTest1 = "<-Oh Hello->";
    const string stringToTest2 = "!-Noice-!";
    const string stringToTest3 = "<-This Is A Test->";
    const string stringToTest4 = "!-More Tests-!";
    var port = new TestConnection(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var test1 = port.Send(new SomeCommandWithResponse(stringToTest1));
    var test2 = port.Send(new SomeCommandWithResponse2(stringToTest2));
    var test3 = port.Send(new SomeCommandWithResponse(stringToTest3));
    var test4 = port.Send(new SomeCommandWithResponse2(stringToTest4));
    await Task.WhenAll(test1, test2, test3, test4);
    Assert.Multiple(() =>
    {
      Assert.That(test1.Result, Is.Not.Null);
      Assert.That(test2.Result, Is.Not.Null);
      Assert.That(test3.Result, Is.Not.Null);
      Assert.That(test4.Result, Is.Not.Null);
    });

    // since the TestPort does not guarantee the responses in order, it can only guarantee
    // that the proper parser is used for each of the commands
    // ex.: two parsers expecting a "<- ->" type string are added to the queue
    // the first result coming from the port will be parsed with the first available parser
    // so the result may not match.
    Assert.Multiple(() =>
    {
      Assert.That(test1.Result.Response, Is.EqualTo("<-"));
      Assert.That(test2.Result.OtherResponse, Is.EqualTo("!-"));
      Assert.That(test3.Result.Response, Is.EqualTo("<-"));
      Assert.That(test4.Result.OtherResponse, Is.EqualTo("!-"));
    });

    // var currentBuffer = await port.DataBufferState.FirstAsync();
    // Assert.That(currentBuffer, Is.Empty);
  }

  [Test]
  [CancelAfter(5000)]
  public async Task AresSerialPort_Previous_Stream_Observable_Fires_Once_New_Command_Appears()
  {
    const string stringToTest = "<-Oh Hello->";
    const string stringToTest2 = "<-This Is A Test->";
    var port = new TestConnection(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var responseObserver = port.GetTransactionStream<SomeResponse>();
    var getTest1FirstResponse = responseObserver.Take(1);
    _ = port.Send(new SomeCommandWithStreamedResponse(stringToTest));
    var test1ObservableFirstResponse = await getTest1FirstResponse;
    var secondResponseWaiter = Task.Run(async () =>
    {
      var test1ObservableSecondResponse = await responseObserver.Take(1);
      return test1ObservableSecondResponse;
    });

    _ = port.Send(new SomeCommandWithResponse(stringToTest2));


    var test2ObservableFirestResponse = await responseObserver.Take(1);
    var test1ObservableSecondResponse = await secondResponseWaiter;
    Assert.Multiple(() =>
    {
      Assert.That(test1ObservableFirstResponse.Response.Response, Is.EqualTo(stringToTest));
      Assert.That(test2ObservableFirestResponse.Response.Response, Is.EqualTo(stringToTest2));
      Assert.That(test1ObservableSecondResponse.Response.Response, Is.EqualTo(stringToTest2));
    });

    // var currentBuffer = await port.DataBufferState.FirstAsync();
    // Assert.That(currentBuffer, Is.Empty);
  }

  [Test]
  [CancelAfter(5000)]
  public async Task AresSerialPort_Subscription_To_Response_Stream_Works_Without_Sending_Command()
  {
    const string stringToTest = "<-Oh Hello->";
    const string stringToTest2 = "<-This Is A Test->";
    var port = new TestConnection(new SerialPortConnectionInfo(0, Parity.Even, 0, StopBits.None));
    var test1Observable = port.GetTransactionStream<SomeResponse>();
    var test2Observable = port.GetTransactionStream<SomeResponse>();
    var test1ObservableResponseWaiter = Task.Run(async () =>
    {
      var test1ObservableSecondResponse = await test1Observable.Take(1);
      return test1ObservableSecondResponse;
    });

    _ = port.Send(new SomeCommandWithStreamedResponse(stringToTest2));

    var test2ObservableFirstResponse = await test2Observable.Take(1);
    var test1ObservableSecondResponse = await test1ObservableResponseWaiter;
    var test1ObservableResponseWaiter2 = Task.Run(async () =>
    {
      var test1ObservableSecondResponse2 = await test1Observable.Take(1);
      return test1ObservableSecondResponse2;
    });

    var test3Task = await port.Send(new SomeCommandWithResponse(stringToTest));
    var test1ObservableSecondResponse2 = await test1ObservableResponseWaiter2;
    Assert.Multiple(() =>
    {
      Assert.That(test1ObservableSecondResponse2.Response.Response, Is.EqualTo(stringToTest));
      Assert.That(test2ObservableFirstResponse.Response.Response, Is.EqualTo(stringToTest2));
      Assert.That(test1ObservableSecondResponse.Response.Response, Is.EqualTo(stringToTest2));
    });

    Assert.That(port.BufferEmpty, Is.True);
  }
}
internal class SomeResponse : SerialResponse
{
  public SomeResponse(string response)
  {
    Response = response;
  }

  public string Response { get; }
}
internal class SomeResponse2 : SerialResponse
{
  public SomeResponse2(string otherResponse)
  {
    OtherResponse = otherResponse;
  }

  public string OtherResponse { get; }
}
internal class SomeResponseParser : SerialResponseParser<SomeResponse>
{
  public override bool TryParseResponse(byte[] bufferArr, out SomeResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    try
    {
      var parsed = Encoding.ASCII.GetString(bufferArr);
      var startIdx = parsed.IndexOf("<-", StringComparison.InvariantCultureIgnoreCase);
      var endIdx = startIdx >= 0 ? parsed.IndexOf("->", startIdx, StringComparison.InvariantCultureIgnoreCase) : -1;
      endIdx = endIdx > 0 ? endIdx + "->".Length : endIdx;
      if (startIdx < 0 || endIdx < 0 || string.IsNullOrEmpty(parsed))
      {
        response = null;
        dataToRemove = null;
        return false;
      }

      response = new SomeResponse(parsed[startIdx..endIdx]);
      dataToRemove = new ArraySegment<byte>(bufferArr, startIdx, endIdx - startIdx);
      return true;
    }
    catch (Exception)
    {
      response = null;
      dataToRemove = null;
      return false;
    }
  }
}
internal class SomeResponse2Parser : SerialResponseParser<SomeResponse2>
{
  public override bool TryParseResponse(byte[] bufferArr, out SomeResponse2? response, out ArraySegment<byte>? dataToRemove)
  {
    try
    {
      var parsed = Encoding.ASCII.GetString(bufferArr.ToArray());
      var startIdx = parsed.IndexOf("!-", StringComparison.InvariantCultureIgnoreCase);
      var endIdx = startIdx >= 0 ? parsed.IndexOf("-!", startIdx, StringComparison.InvariantCultureIgnoreCase) : -1;
      endIdx = endIdx > 0 ? endIdx + "-!".Length : endIdx;
      if (startIdx < 0 || endIdx <= 0 || string.IsNullOrEmpty(parsed))
      {
        response = null;
        dataToRemove = null;
        return false;
      }

      response = new SomeResponse2(parsed[startIdx..endIdx]);
      dataToRemove = new ArraySegment<byte>(bufferArr.ToArray(), startIdx, endIdx - startIdx);
      return true;
    }
    catch (Exception)
    {
      response = null;
      dataToRemove = null;
      return false;
    }
  }
}
internal class SomeCommandWithResponse : SerialCommandWithResponse<SomeResponse>
{
  public SomeCommandWithResponse(string message) : base(new SomeResponseParser())
  {
    Message = message;
  }

  public string Message { get; }

  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes(Message);
}
internal class SomeCommandWithStreamedResponse : SerialCommandWithStreamedResponse<SomeResponse>
{
  public SomeCommandWithStreamedResponse(string message) : base(new SomeResponseParser())
  {
    Message = message;
  }

  public string Message { get; }

  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes(Message);
}
internal class SomeCommandWithResponse2 : SerialCommandWithResponse<SomeResponse2>
{
  public SomeCommandWithResponse2(string otherMessage) : base(new SomeResponse2Parser())
  {
    OtherMessage = otherMessage;
  }

  public string OtherMessage { get; }

  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes(OtherMessage);
}
public class TestConnection : AresSerialSimConnection
{
  private bool _isProcessing;

  public TestConnection(SerialPortConnectionInfo connectionInfo) : base(connectionInfo, "SIM1")
  {
  }

  public override void SendInternally(byte[] bytes)
  {
    // having the _isProcessing check will make the test fail if the thread adding to the buffer is the
    // same one as the one processing the buffer
    if (_isProcessing)
      return;

    _isProcessing = true;
    var random = new Random();
    Task.Delay(random.Next(100, 300)).ContinueWith(_ =>
    {
      AddDataReceived(bytes);
      _isProcessing = false;
    });
  }
}
public class TestPort2 : AresSerialSimConnection
{

  public TestPort2(SerialPortConnectionInfo connectionInfo) : base(connectionInfo, "SIM2")
  {
  }

  public override void SendInternally(byte[] bytes)
  {
    var slice1 = bytes[..1];
    var slice2 = bytes[1..2];
    var slice3 = bytes[2..];
    Task.Run(async () =>
    {
      AddDataReceived(slice1);
      await Task.Delay(100);
      AddDataReceived(slice2);
      await Task.Delay(200);
      AddDataReceived(slice3);
    });
  }
}
