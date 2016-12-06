<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:template match="/Metadata">
    <collection xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/1.1/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://purl.org/dc/terms/1.1/ http://dublincore.org/schemas/xmls/qdc/2008/02/11/dcterms.xsd http://purl.org/dc/elements/1.1/ http://dublincore.org/schemas/xmls/qdc/2008/02/11/dc.xsd">
      <record>
        <dc:title><xsl:value-of select="Title"/></dc:title>
        <dc:creator><xsl:value-of select="Author"/></dc:creator>
        <dc:publisher>
          <xsl:value-of select="Publisher"/>
        </dc:publisher>
        <dc:date>
          <xsl:value-of select="PublicationDate"/>
        </dc:date>
        <dc:language>eng</dc:language>
      </record>
    </collection>
  </xsl:template>
</xsl:stylesheet>