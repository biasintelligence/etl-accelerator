CREATE XML SCHEMA COLLECTION [dbo].[EventArgsCollection]
    AS N'<xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:t="EventArgs.XSD" targetNamespace="EventArgs.XSD">
  <xsd:element name="EventArgs" type="t:EventArgs" />
  <xsd:complexType name="EventArgs">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence />
        <xsd:attribute name="Source" type="xsd:string" use="required" />
        <xsd:attribute name="PeriodGrain" type="t:PeriodGrain" use="required" />
        <xsd:attribute name="Period" type="xsd:int" use="required" />
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:simpleType name="PeriodGrain">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="Year" />
      <xsd:enumeration value="Quarter" />
      <xsd:enumeration value="Month" />
      <xsd:enumeration value="Week" />
      <xsd:enumeration value="Day" />
      <xsd:enumeration value="Hour" />
      <xsd:enumeration value="Minute" />
    </xsd:restriction>
  </xsd:simpleType>
</xsd:schema>';

