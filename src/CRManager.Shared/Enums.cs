namespace CRManager.Shared;

public enum CardNetwork
{
    Visa = 1,
    MasterCard = 2,
    AmericanExpress = 3,
    Discover = 4
}

public enum TransactionCategory
{
    General = 1,
    Groceries = 2,
    Dining = 3,
    Travel = 4,
    Subscriptions = 5,
    Utilities = 6,
    Shopping = 7,
    Gas = 8
}

public enum TransactionStatus
{
    Pending = 1,
    Posted = 2,
    Disputed = 3,
    Cancelled = 4
}

public enum StatementStatus
{
    Open = 1,
    PaidInFull = 2,
    Overdue = 3
}

public enum TransactionType
{
    Purchase = 1,
    Payment = 2
}

public static class BankHelper
{
    public static string? GetArabicName(string? bankName)
    {
        if (string.IsNullOrWhiteSpace(bankName))
            return null;

        var clean = bankName.Trim().ToLowerInvariant();

        if (clean.Contains("cib") || clean.Contains("تجاري دولي"))
            return "البنك التجاري الدولي";
        if (clean.Contains("nbe") || clean.Contains("ahli") || clean.Contains("ahly") || clean.Contains("أهلي") || clean.Contains("اهلي"))
            return "البنك الأهلي المصري";
        if (clean.Contains("misr") || clean.Contains("bm") || clean.Contains("مصر"))
            return "بنك مصر";
        if (clean.Contains("qnb") || clean.Contains("قطر وطني"))
            return "بنك قطر الوطني";
        if (clean.Contains("alex") || clean.Contains("إسكندرية") || clean.Contains("اسكندرية"))
            return "بنك الإسكندرية";
        if (clean.Contains("hsbc") || clean.Contains("اتش اس بي سي") || clean.Contains("إتش إس بي سي"))
            return "إتش إس بي سي";
        if (clean.Contains("hdb") || clean.Contains("housing") || clean.Contains("taameer") || clean.Contains("iskan") || clean.Contains("تعمير وإسكان") || clean.Contains("تعمير واسكان"))
            return "بنك التعمير والإسكان";
        if (clean.Contains("caire") || clean.Contains("bdc") || clean.Contains("قاهرة"))
            return "بنك القاهرة";
        if (clean.Contains("fab") || clean.Contains("أبوظبي أول") || clean.Contains("ابوظبي اول"))
            return "بنك أبوظبي الأول";
        if (clean.Contains("arab") || clean.Contains("عربي"))
            return "البنك العربي";
        if (clean.Contains("nbd") || clean.Contains("emirates") || clean.Contains("إمارات دبي") || clean.Contains("امارات دبي"))
            return "بنك الإمارات دبي الوطني";
        if (clean.Contains("adib") || clean.Contains("أبوظبي إسلامي") || clean.Contains("ابوظبي اسلامي"))
            return "مصرف أبوظبي الإسلامي";
        if (clean.Contains("saib") || clean.Contains("صائب") || clean.Contains("سيب"))
            return "بنك الشركة المصرفية (saib)";
        if (clean.Contains("abc") || clean.Contains("اي بي سي") || clean.Contains("مؤسسة عربية مصرفية"))
            return "بنك المؤسسة العربية المصرفية (ABC)";
        if (clean.Contains("midbank") || clean.Contains("mid bank") || clean.Contains("ميد"))
            return "ميد بنك";
        if (clean.Contains("faisal") || clean.Contains("فيصل"))
            return "بنك فيصل الإسلامي";
        if (clean.Contains("baraka") || clean.Contains("بركة"))
            return "بنك البركة";
        if (clean.Contains("attijari") || clean.Contains("تجاري وفا"))
            return "التجاري وفا بنك";
        if (clean.Contains("ebank") || clean.Contains("صادرات"))
            return "البنك المصري لتنمية الصادرات";

        return null;
    }
}
