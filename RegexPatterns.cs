// author: Matous Havlicek  (xhavli66)
// file for regex patterns (used for parsing messages and responses)

namespace ipk25_chat;

// class defining regex patterns for parsing messages and responses
public class RegexPatterns
{
    // regex to check length and correct characters of these components
    private static readonly string ID = @"(?i)[A-Za-z0-9_-]{1,20}";
    private static readonly string SECRET = @"(?i)[A-Za-z0-9_-]{1,128}";
    private static readonly string CONTENT = @"(?i)[\x21-\x7E \n]{1,60000}"; // VCHAR, SP, LF
    private static readonly string DNAME = @"(?i)[\x21-\x7E.]{1,20}"; // only VCHAR (printable ASCII)
    

    // middle parts of the content
    private static readonly string IS = @"(?i)\sIS\s";

    // received messages regex
    public static readonly string contentMessage = $@"(?i)^MSG FROM\s({DNAME}){IS}({CONTENT})$";
    public static readonly string contentError = $@"(?i)^ERR FROM\s({DNAME}){IS}({CONTENT})$";
    public static readonly string contentBye = $@"(?i)^BYE FROM\s({DNAME})$";
    public static readonly string contentReplyOK = $@"(?i)^REPLY\sOK{IS}({CONTENT})$";
    public static readonly string contentReplyNOK = $@"(?i)^REPLY\sNOK{IS}({CONTENT})$";
    // regex for input commands
    public static readonly string contentAuthRegex = $@"(?i)^/AUTH\s{ID}\s{SECRET}\s{DNAME}$";
    public static readonly string contentJoinRegex = $@"(?i)^/JOIN\s({ID})$";
    public static readonly string contentRenameRegex = $@"(?i)^/RENAME\s({DNAME})$";
}