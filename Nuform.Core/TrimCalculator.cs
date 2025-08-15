using System;
using System.Collections.Generic;

namespace Nuform.Core;

public static class TrimCalculator
{
    public record TrimResult(decimal JTrimLf, int OutsideCorners, int InsideCorners, int EndCaps);

    public static TrimResult Calculate(IEnumerable<Room> rooms, bool useCeilingPanels)
    {
        if (rooms is null) throw new ArgumentNullException(nameof(rooms));

        decimal perimeter = 0;
        foreach (var room in rooms)
        {
            perimeter += (decimal)(2 * (room.LengthFt + room.WidthFt));
        }

        decimal jTrimLf = Math.Ceiling(perimeter);
        int outsideCorners = useCeilingPanels ? 4 : 0;

        return new TrimResult(jTrimLf, outsideCorners, 0, 0);
    }
}
