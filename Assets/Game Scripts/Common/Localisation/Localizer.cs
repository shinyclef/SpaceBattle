public class Localizer
{
    public LocalStringDto stringDto;

    public static LocalStringDto Strings => I.stringDto;
    private static Localizer I;

    static Localizer()
    {
        new Localizer();
    }

    public Localizer()
    {
        SetDefaultStrings();
        I = this;
    }

    private void SetDefaultStrings()
    {
        stringDto = LocalStringDto.GetDefault();
    }
}