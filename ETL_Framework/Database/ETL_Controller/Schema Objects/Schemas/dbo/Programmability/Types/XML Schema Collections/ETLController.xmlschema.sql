CREATE XML SCHEMA COLLECTION [dbo].[ETLController]
    AS N'<xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:t="ETLController.XSD" targetNamespace="ETLController.XSD" elementFormDefault="qualified">
  <xsd:element name="Attributes" type="t:Attributes" />
  <xsd:element name="Context" type="t:Context" />
  <xsd:element name="Counters">
    <xsd:complexType>
      <xsd:complexContent>
        <xsd:restriction base="xsd:anyType">
          <xsd:sequence>
            <xsd:element name="Counter" minOccurs="0" maxOccurs="unbounded">
              <xsd:complexType>
                <xsd:simpleContent>
                  <xsd:extension base="xsd:string">
                    <xsd:attribute name="Name" type="xsd:string" use="required" />
                    <xsd:attribute name="RunID" type="xsd:int" use="required" />
                  </xsd:extension>
                </xsd:simpleContent>
              </xsd:complexType>
            </xsd:element>
          </xsd:sequence>
          <xsd:attribute name="BatchID" type="xsd:int" use="required" />
          <xsd:attribute name="StepID" type="xsd:int" />
          <xsd:attribute name="ConstID" type="xsd:int" />
          <xsd:attribute name="RunID" type="xsd:int" use="required" />
        </xsd:restriction>
      </xsd:complexContent>
    </xsd:complexType>
  </xsd:element>
  <xsd:element name="Header" type="t:Header" />
  <xsd:element name="ProcessInfo">
    <xsd:complexType>
      <xsd:complexContent>
        <xsd:restriction base="xsd:anyType">
          <xsd:sequence>
            <xsd:element name="Header" type="t:Header" />
            <xsd:element name="Message">
              <xsd:complexType>
                <xsd:simpleContent>
                  <xsd:extension base="xsd:string">
                    <xsd:attribute name="Error" type="xsd:int" />
                  </xsd:extension>
                </xsd:simpleContent>
              </xsd:complexType>
            </xsd:element>
          </xsd:sequence>
          <xsd:attribute name="Error" type="xsd:int" />
        </xsd:restriction>
      </xsd:complexContent>
    </xsd:complexType>
  </xsd:element>
  <xsd:element name="ProcessReceipt">
    <xsd:complexType>
      <xsd:complexContent>
        <xsd:restriction base="xsd:anyType">
          <xsd:sequence>
            <xsd:element name="Header" type="t:Header" />
            <xsd:element name="Status">
              <xsd:complexType>
                <xsd:complexContent>
                  <xsd:restriction base="xsd:anyType">
                    <xsd:sequence>
                      <xsd:element name="msg" type="xsd:string" minOccurs="0" />
                    </xsd:sequence>
                    <xsd:attribute name="StatusID" type="xsd:int" use="required" />
                    <xsd:attribute name="Error" type="xsd:int" use="required" />
                  </xsd:restriction>
                </xsd:complexContent>
              </xsd:complexType>
            </xsd:element>
          </xsd:sequence>
        </xsd:restriction>
      </xsd:complexContent>
    </xsd:complexType>
  </xsd:element>
  <xsd:element name="ProcessRequest">
    <xsd:complexType>
      <xsd:complexContent>
        <xsd:restriction base="xsd:anyType">
          <xsd:sequence>
            <xsd:element name="Header" type="t:Header" />
            <xsd:element name="SrcConversation" type="xsd:string" minOccurs="0" />
            <xsd:element name="SrcConversationGrp" type="xsd:string" minOccurs="0" />
            <xsd:element name="DstConversation" type="xsd:string" minOccurs="0" />
            <xsd:element name="DstConversationGrp" type="xsd:string" minOccurs="0" />
            <xsd:element name="Context" type="t:Context" minOccurs="0" />
          </xsd:sequence>
        </xsd:restriction>
      </xsd:complexContent>
    </xsd:complexType>
  </xsd:element>
  <xsd:complexType name="Attributes">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="Attribute" minOccurs="0" maxOccurs="unbounded">
            <xsd:complexType>
              <xsd:simpleContent>
                <xsd:extension base="xsd:string">
                  <xsd:attribute name="Name" type="xsd:string" use="required" />
                </xsd:extension>
              </xsd:simpleContent>
            </xsd:complexType>
          </xsd:element>
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Constraints">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="Constraint" minOccurs="0" maxOccurs="unbounded">
            <xsd:complexType>
              <xsd:complexContent>
                <xsd:restriction base="xsd:anyType">
                  <xsd:sequence>
                    <xsd:element name="Process" type="t:Process" />
                    <xsd:element name="Attributes" type="t:Attributes" minOccurs="0" maxOccurs="unbounded" />
                  </xsd:sequence>
                  <xsd:attribute name="ConstID" type="xsd:int" use="required" />
                  <xsd:attribute name="ConstOrder" type="xsd:string" />
                  <xsd:attribute name="WaitPeriod" type="xsd:int" />
                  <xsd:attribute name="Disabled" type="xsd:int" />
                  <xsd:attribute name="Ping" type="xsd:int" />
                </xsd:restriction>
              </xsd:complexContent>
            </xsd:complexType>
          </xsd:element>
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Context">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="OnSuccess" type="t:Process" minOccurs="0" />
          <xsd:element name="OnFailure" type="t:Process" minOccurs="0" />
          <xsd:element name="Attributes" type="t:Attributes" minOccurs="0" maxOccurs="unbounded" />
          <xsd:element name="Constraints" type="t:Constraints" minOccurs="0" maxOccurs="unbounded" />
          <xsd:element name="Steps" minOccurs="0">
            <xsd:complexType>
              <xsd:complexContent>
                <xsd:restriction base="xsd:anyType">
                  <xsd:sequence>
                    <xsd:element name="Step" minOccurs="0" maxOccurs="unbounded">
                      <xsd:complexType>
                        <xsd:complexContent>
                          <xsd:restriction base="xsd:anyType">
                            <xsd:sequence>
                              <xsd:element name="Process" type="t:Process" />
                              <xsd:element name="OnSuccess" type="t:Process" minOccurs="0" />
                              <xsd:element name="OnFailure" type="t:Process" minOccurs="0" />
                              <xsd:element name="Attributes" type="t:Attributes" minOccurs="0" maxOccurs="unbounded" />
                              <xsd:element name="Constraints" type="t:Constraints" minOccurs="0" maxOccurs="unbounded" />
                            </xsd:sequence>
                            <xsd:attribute name="StepID" type="xsd:int" use="required" />
                            <xsd:attribute name="StepName" type="xsd:string" />
                            <xsd:attribute name="StepDesc" type="xsd:string" />
                            <xsd:attribute name="StepOrder" type="xsd:string" />
                            <xsd:attribute name="IgnoreErr" type="xsd:int" />
                            <xsd:attribute name="Restart" type="xsd:int" />
                            <xsd:attribute name="Disabled" type="xsd:int" />
                            <xsd:attribute name="SeqGroup" type="xsd:string" />
                            <xsd:attribute name="PriGroup" type="xsd:string" />
                            <xsd:attribute name="Retry" type="xsd:int" />
                            <xsd:attribute name="Delay" type="xsd:int" />
                            <xsd:attribute name="LoopGroup" type="xsd:string" />
                          </xsd:restriction>
                        </xsd:complexContent>
                      </xsd:complexType>
                    </xsd:element>
                  </xsd:sequence>
                </xsd:restriction>
              </xsd:complexContent>
            </xsd:complexType>
          </xsd:element>
        </xsd:sequence>
        <xsd:attribute name="BatchID" type="xsd:int" use="required" />
        <xsd:attribute name="BatchName" type="xsd:string" />
        <xsd:attribute name="BatchDesc" type="xsd:string" />
        <xsd:attribute name="IgnoreErr" type="xsd:int" />
        <xsd:attribute name="Restart" type="xsd:int" />
        <xsd:attribute name="Disabled" type="xsd:int" />
        <xsd:attribute name="MaxThread" type="xsd:int" />
        <xsd:attribute name="Timeout" type="xsd:int" />
        <xsd:attribute name="Lifetime" type="xsd:int" />
        <xsd:attribute name="Ping" type="xsd:int" />
        <xsd:attribute name="HistRet" type="xsd:int" />
        <xsd:attribute name="Retry" type="xsd:int" />
        <xsd:attribute name="Delay" type="xsd:int" />
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Header">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence />
        <xsd:attribute name="BatchID" type="xsd:int" use="required" />
        <xsd:attribute name="StepID" type="xsd:int" />
        <xsd:attribute name="ConstID" type="xsd:int" />
        <xsd:attribute name="RunID" type="xsd:int" use="required" />
        <xsd:attribute name="Options" type="xsd:int" />
        <xsd:attribute name="Scope" type="xsd:int" />
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Process">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="Process" type="xsd:string" minOccurs="0" />
          <xsd:element name="Param" type="xsd:string" minOccurs="0" />
        </xsd:sequence>
        <xsd:attribute name="ProcessID" type="xsd:int" use="required" />
        <xsd:attribute name="ScopeID" type="xsd:int" />
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
</xsd:schema>';

