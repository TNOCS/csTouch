/***********************************************************************
Copyright DevZest, 2009 (http://www.devzest.com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

This program is part of WPF Docking, a commercial undo/redo-able
docking library, which you can download from http://www.devzest.com.
You can obtain a Free Feature License of WPF Docking through
installed License Console, FREE OF CHARGE, with the benefit of writing
proprietary software, along with commercial product quality
documentation, upgrade and free technical support.
**********************************************************************/

using System;

namespace DevZest.Windows
{
    /// <summary>Describes the kind of value that a <see cref="SplitterDistance"/> object is holding.</summary>
    /// <remarks>
    /// <see cref="SplitterUnitType.Star"/> sizing is used to distribute available space by weighted proportions:
    /// sizeInPixel = availableSizeInPixel * weightedProportion / (weightedProportion + 1).
    /// </remarks>
    public enum SplitterUnitType
    {
        /// <summary>The value is expressed as a pixel.</summary>
        Pixel,
        /// <summary>The value is expressed as a weighted proportion of available space.</summary>
        Star
    }
}
