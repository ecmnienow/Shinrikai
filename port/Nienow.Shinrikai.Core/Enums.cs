namespace Nienow.Shinrikai.Core;

public enum EJLPT
{
    None = 0,
    N5 = 1,
    N4 = 2,
    N3 = 3,
    N2 = 4,
    N1 = 5
}

public enum EMode
{
    Mistakes,
    Normal
}

public enum ESex
{
    Male = 0,
    Female = 1
}

public class Response
{
    public bool OK { get; set; }
    public string? Message { get; set; }

    public static Response Ok(string? message = null) => new() { OK = true, Message = message };
    public static Response Fail(string message) => new() { OK = false, Message = message };
}
