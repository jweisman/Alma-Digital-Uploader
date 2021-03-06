﻿<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:template match="/Metadata">
    <collection>
      <record>
        <leader>     aas          a     </leader>
        <datafield tag="020" ind1=" " ind2=" ">
          <subfield code="a">
            <xsl:value-of select="ISBN"/>
          </subfield>
        </datafield>
      <datafield tag="100" ind1="1" ind2=" ">
        <subfield code="a">
          <xsl:value-of select="Author"/>
        </subfield>
      </datafield>
      <datafield tag="245" ind1="1" ind2="2">
        <subfield code="a">
          <xsl:value-of select="Title"/>
        </subfield>
      </datafield>
      <datafield tag="260" ind1=" " ind2=" ">
        <subfield code="b">
          <xsl:value-of select="Publisher"/>
        </subfield>
        <subfield code="c">
          <xsl:value-of select="PublicationDate"/>
        </subfield>
      </datafield>
      <datafield tag="562" ind1=" " ind2=" ">
        <subfield code="a">
          <xsl:value-of select="Notes"/>
        </subfield>
      </datafield>
	  </record>
    </collection>
  </xsl:template>
</xsl:stylesheet>