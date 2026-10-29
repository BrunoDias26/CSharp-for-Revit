namespace CSharp_for_Revit.Utilities
{
    /// <summary>
    /// Methods about MEP connectors. Works for any MEP element: fabrication parts, pipes, fittings, accessories.
    /// </summary>
    public static class Connector_Utilities
    {
        #region Get

        /// <summary>
        /// Returns all the connectors of an MEP element.
        /// </summary>
        /// <param name="element">A FabricationPart, an MEPCurve (Pipe, Duct...) or a FamilyInstance with connectors.</param>
        /// <returns>A list of connectors (empty if the element is null or has no connectors).</returns>
        public static List<Connector> GetConnectors(Element element)
        {
            var connectors = new List<Connector>();

            if (element is null) { return connectors; }

            // Each kind of MEP element keeps its connectors in a different place.
            // "is X name" checks the type and, if true, gives a variable already of that type.
            ConnectorManager connectorManager = null;

            if (element is FabricationPart fabricationPart)
            {
                connectorManager = fabricationPart.ConnectorManager;
            }
            else if (element is MEPCurve mepCurve)
            {
                // Pipe, Duct, Conduit, CableTray...
                connectorManager = mepCurve.ConnectorManager;
            }
            else if (element is FamilyInstance familyInstance)
            {
                // Fittings, accessories, equipment. MEPModel is null in families without connectors.
                if (familyInstance.MEPModel is not null)
                {
                    connectorManager = familyInstance.MEPModel.ConnectorManager;
                }
            }

            // Not an MEP element, or a family without connectors
            if (connectorManager is null) { return connectors; }

            // ConnectorSet is an old collection type (without <T>): Cast tells C# that each item is a Connector
            connectors = connectorManager.Connectors
                .Cast<Connector>()
                .ToList();

            return connectors;
        }

        /// <summary>
        /// Returns the positions of the end connectors: the physical ends of the element.
        /// </summary>
        /// <param name="element">An MEP element (see GetConnectors).</param>
        /// <returns>A list of points in model coordinates, in feet (empty if no end connectors).</returns>
        public static List<XYZ> GetEndPoints(Element element)
        {
            var endPoints = new List<XYZ>();

            foreach (var connector in GetConnectors(element))
            {
                // Only real ends. Skips Curve connectors (taps along a pipe) and Logical ones,
                // which have no position: reading Origin on a Logical connector throws an exception.
                if (connector.ConnectorType == ConnectorType.End)
                {
                    endPoints.Add(connector.Origin);
                }
            }

            return endPoints;
        }

        /// <summary>
        /// Returns the direction from one end of the element to the other, as a unit vector (length 1).
        /// Only meaningful for straight parts: on an elbow, the two ends form a false diagonal.
        /// </summary>
        /// <param name="element">An MEP element (see GetConnectors).</param>
        /// <returns>The unit vector, or null if the element doesn't have exactly 2 end connectors
        /// or both ends are in the same place.</returns>
        public static XYZ GetDirection(Element element)
        {
            var endPoints = GetEndPoints(element);

            // Tees (3), crosses (4), caps (1): no single direction
            if (endPoints.Count != 2) { return null; }

            // The order of the connectors is not guaranteed, so the vector may point either way.
            // For horizontal / vertical / riser it doesn't matter: the next step compares angles, not the sign.
            var vector = endPoints[1] - endPoints[0];

            // Both ends in the same place: there is no direction to give
            if (vector.IsZeroLength() == true) { return null; }

            // Normalize = same direction, length 1: easier to compare angles later
            return vector.Normalize();
        }

        #endregion
    }
}
