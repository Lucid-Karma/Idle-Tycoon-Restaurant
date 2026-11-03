// The bacon station's flat-top: bacon only (patties stay on the pan, so every ingredient has one place to go).
public class Griddle : HeatStation
{
    public override bool Takes(EdibleBase food) => food is Bacon;
}
