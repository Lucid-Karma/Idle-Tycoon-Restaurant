// The fry station's fryer: a potato goes in, fries come out.
public class Fryer : HeatStation
{
    public override bool Takes(EdibleBase food) => food is Fries;
    protected override bool DeepFries => true;
}
