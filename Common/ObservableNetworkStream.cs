using System.Reactive.Disposables;
using System.Text;

namespace Common;

public class ObservableStreamReader : IObservable<string>
{
    private readonly Stream _baseStream;
    private readonly string _id;

    private ObservableStreamReader(Stream baseStream, string id)
    {
        _baseStream = baseStream;
        _id = id;
    }

    public static ObservableStreamReader Create(Stream baseStream, string id)
    {
        return new ObservableStreamReader(baseStream, id);
    }

    public IDisposable Subscribe(IObserver<string> observer)
    {
        var thread = new Thread(o =>
        {
            string bufferedMessage = "";
            var obs = (IObserver<string>) o!;
            while (true)
            {
                if (!_baseStream.CanRead)
                {
                    break;
                }

                try
                {
                    int currentByte = _baseStream.ReadByte();

                    if (currentByte == -1)
                    {
                        break;
                    }

                    if (currentByte == 10)
                    {
                        if (!string.IsNullOrEmpty(bufferedMessage))
                        {
                            obs.OnNext(bufferedMessage);
                            bufferedMessage = "";
                        }
                    }
                    else
                    {
                        //Console.ResetColor();
                        //Console.WriteLine($"{_id} {currentByte}");
                        var car = Encoding.UTF8.GetChars(new[] { (byte)currentByte })[0];
                        bufferedMessage += car;
                    }
                }
                catch (Exception e)
                {
                    break;
                }
            }
            observer.OnCompleted();
        });
        thread.Start(observer);
        return Disposable.Create(thread.Interrupt);
    }
}