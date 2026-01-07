-- Northwind PostgreSQL Database Script
-- Reference: instnwnd.sql (SQL Server version)
-- Tables: Employees, Region, Territories, EmployeeTerritories

-- Drop existing tables
DROP TABLE IF EXISTS EmployeeTerritories;
DROP TABLE IF EXISTS Territories;
DROP TABLE IF EXISTS Region;
DROP TABLE IF EXISTS Employees;

-- Create Tables
CREATE TABLE Employees (
    EmployeeID SERIAL PRIMARY KEY,
    LastName VARCHAR(20) NOT NULL,
    FirstName VARCHAR(10) NOT NULL,
    Title VARCHAR(30),
    TitleOfCourtesy VARCHAR(25),
    BirthDate TIMESTAMP,
    HireDate TIMESTAMP,
    Address VARCHAR(60),
    City VARCHAR(15),
    Region VARCHAR(15),
    PostalCode VARCHAR(10),
    Country VARCHAR(15),
    HomePhone VARCHAR(24),
    Extension VARCHAR(4),
    Notes TEXT,
    ReportsTo INTEGER,
    FOREIGN KEY (ReportsTo) REFERENCES Employees(EmployeeID)
);
INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(2,'Fuller','Andrew','Vice President, Sales','Dr.','02/19/1952','08/14/1992','908 W. Capital Way','Tacoma','WA','98401','USA','(206) 555-9482','3457','Andrew received his BTS commercial in 1974 and a Ph.D. in international marketing from the University of Dallas in 1981.  He is fluent in French and Italian and reads German.  He joined the company as a sales representative, was promoted to sales manager in January 1992 and to vice president of sales in March 1993.  Andrew is a member of the Sales Management Roundtable, the Seattle Chamber of Commerce, and the Pacific Rim Importers Association.',NULL); INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(1,'Davolio','Nancy','Sales Representative','Ms.','12/08/1948','05/01/1992','507 - 20th Ave. E.Apt. 2A','Seattle','WA','98122','USA','(206) 555-9857','5467','Education includes a BA in psychology from Colorado State University in 1970.  She also completed "The Art of the Cold Call."  Nancy is a member of Toastmasters International.',2); INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(3,'Leverling','Janet','Sales Representative','Ms.','08/30/1963','04/01/1992','722 Moss Bay Blvd.','Kirkland','WA','98033','USA','(206) 555-3412','3355','Janet has a BS degree in chemistry from Boston College (1984).  She has also completed a certificate program in food retailing management.  Janet was hired as a sales associate in 1991 and promoted to sales representative in February 1992.',2); INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(4,'Peacock','Margaret','Sales Representative','Mrs.','09/19/1937','05/03/1993','4110 Old Redmond Rd.','Redmond','WA','98052','USA','(206) 555-8122','5176','Margaret holds a BA in English literature from Concordia College (1958) and an MA from the American Institute of Culinary Arts (1966).  She was assigned to the London office temporarily from July through November 1992.',2);
INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(5,'Buchanan','Steven','Sales Manager','Mr.','03/04/1955','10/17/1993','14 Garrett Hill','London',NULL,'SW1 8JR','UK','(71) 555-4848','3453','Steven Buchanan graduated from St. Andrews University, Scotland, with a BSC degree in 1976.  Upon joining the company as a sales representative in 1992, he spent 6 months in an orientation program at the Seattle office and then returned to his permanent post in London.  He was promoted to sales manager in March 1993.  Mr. Buchanan has completed the courses "Successful Telemarketing" and "International Sales Management."  He is fluent in French.',2); INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(8,'Callahan','Laura','Inside Sales Coordinator','Ms.','01/09/1958','03/05/1994','4726 - 11th Ave. N.E.','Seattle','WA','98105','USA','(206) 555-1189','2344','Laura received a BA in psychology from the University of Washington.  She has also completed a course in business French.  She reads and writes French.',2); INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(6,'Suyama','Michael','Sales Representative','Mr.','07/02/1963','10/17/1993','Coventry House Miner Rd.','London',NULL,'EC2 7JR','UK','(71) 555-7773','428','Michael is a graduate of Sussex University (MA, economics, 1983) and the University of California at Los Angeles (MBA, marketing, 1986).  He has also taken the courses "Multi-Cultural Selling" and "Time Management for the Sales Professional."  He is fluent in Japanese and can read and write French, Portuguese, and Spanish.',NULL);
INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(9,'Dodsworth','Anne','Sales Representative','Ms.','01/27/1966','11/15/1994','7 Houndstooth Rd.','London',NULL,'WG2 7LT','UK','(71) 555-4444','452','Anne has a BA degree in English from St. Lawrence College.  She is fluent in French and German.',5);
INSERT INTO Employees(EmployeeID,LastName,FirstName,Title,TitleOfCourtesy,BirthDate,HireDate,Address,City,Region,PostalCode,Country,HomePhone,Extension,Notes,ReportsTo) VALUES(7,'King','Robert','Sales Representative','Mr.','05/29/1960','01/02/1994','Edgeham Hollow Winchester Way','London',NULL,'RG1 9SP','UK','(71) 555-5598','465','Robert King served in the Peace Corps and traveled extensively before completing his degree in English at the University of Michigan in 1992, the year he joined the company.  After completing a course entitled "Selling in Europe," he was transferred to the London office in March 1993.',5);

CREATE INDEX idx_Employees_LastName ON Employees(LastName);
CREATE INDEX idx_Employees_PostalCode ON Employees(PostalCode);
CREATE INDEX idx_Employees_ReportsTo ON Employees(ReportsTo);

CREATE TABLE Region (
    RegionID SERIAL PRIMARY KEY,
    RegionDescription VARCHAR(50) NOT NULL
);

CREATE TABLE Territories (
    TerritoryID VARCHAR(20) PRIMARY KEY,
    TerritoryDescription VARCHAR(50) NOT NULL,
    RegionID INTEGER NOT NULL,
    FOREIGN KEY (RegionID) REFERENCES Region(RegionID)
);

CREATE INDEX idx_Territories_RegionID ON Territories(RegionID);

CREATE TABLE EmployeeTerritories (
    EmployeeID INTEGER NOT NULL,
    TerritoryID VARCHAR(20) NOT NULL,
    PRIMARY KEY (EmployeeID, TerritoryID),
    FOREIGN KEY (EmployeeID) REFERENCES Employees(EmployeeID),
    FOREIGN KEY (TerritoryID) REFERENCES Territories(TerritoryID)
);

CREATE INDEX idx_EmployeeTerritories_EmployeeID ON EmployeeTerritories(EmployeeID);
CREATE INDEX idx_EmployeeTerritories_TerritoryID ON EmployeeTerritories(TerritoryID);

-- Note: INSERT statements should be extracted from instnwnd.sql reference file
-- Use NorthwindSqlParser or execute INSERT statements programmatically


INSERT INTO Region VALUES (1,'Eastern');
INSERT INTO Region VALUES (2,'Western');
INSERT INTO Region VALUES (3,'Northern');
INSERT INTO Region VALUES (4,'Southern');

-- Territories INSERT statements (53 rows)
INSERT INTO Territories VALUES ('01581','Westboro',1);
INSERT INTO Territories VALUES ('01730','Bedford',1);
INSERT INTO Territories VALUES ('01833','Georgetow',1);
INSERT INTO Territories VALUES ('02116','Boston',1);
INSERT INTO Territories VALUES ('02139','Cambridge',1);
INSERT INTO Territories VALUES ('02184','Braintree',1);
INSERT INTO Territories VALUES ('02903','Providence',1);
INSERT INTO Territories VALUES ('03049','Hollis',3);
INSERT INTO Territories VALUES ('03801','Portsmouth',3);
INSERT INTO Territories VALUES ('06897','Wilton',1);
INSERT INTO Territories VALUES ('07960','Morristown',1);
INSERT INTO Territories VALUES ('08837','Edison',1);
INSERT INTO Territories VALUES ('10019','New York',1);
INSERT INTO Territories VALUES ('10038','New York',1);
INSERT INTO Territories VALUES ('11747','Mellvile',1);
INSERT INTO Territories VALUES ('14450','Fairport',1);
INSERT INTO Territories VALUES ('19428','Philadelphia',3);
INSERT INTO Territories VALUES ('19713','Neward',1);
INSERT INTO Territories VALUES ('20852','Rockville',1);
INSERT INTO Territories VALUES ('27403','Greensboro',1);
INSERT INTO Territories VALUES ('27511','Cary',1);
INSERT INTO Territories VALUES ('29202','Columbia',4);
INSERT INTO Territories VALUES ('30346','Atlanta',4);
INSERT INTO Territories VALUES ('31406','Savannah',4);
INSERT INTO Territories VALUES ('32859','Orlando',4);
INSERT INTO Territories VALUES ('33607','Tampa',4);
INSERT INTO Territories VALUES ('40222','Louisville',1);
INSERT INTO Territories VALUES ('44122','Beachwood',3);
INSERT INTO Territories VALUES ('45839','Findlay',3);
INSERT INTO Territories VALUES ('48075','Southfield',3);
INSERT INTO Territories VALUES ('48084','Troy',3);
INSERT INTO Territories VALUES ('48304','Bloomfield Hills',3);
INSERT INTO Territories VALUES ('53404','Racine',3);
INSERT INTO Territories VALUES ('55113','Roseville',3);
INSERT INTO Territories VALUES ('55439','Minneapolis',3);
INSERT INTO Territories VALUES ('60179','Hoffman Estates',2);
INSERT INTO Territories VALUES ('60601','Chicago',2);
INSERT INTO Territories VALUES ('72716','Bentonville',4);
INSERT INTO Territories VALUES ('75234','Dallas',4);
INSERT INTO Territories VALUES ('78759','Austin',4);
INSERT INTO Territories VALUES ('80202','Denver',2);
INSERT INTO Territories VALUES ('80909','Colorado Springs',2);
INSERT INTO Territories VALUES ('85014','Phoenix',2);
INSERT INTO Territories VALUES ('85251','Scottsdale',2);
INSERT INTO Territories VALUES ('90405','Santa Monica',2);
INSERT INTO Territories VALUES ('94025','Menlo Park',2);
INSERT INTO Territories VALUES ('94105','San Francisco',2);
INSERT INTO Territories VALUES ('95008','Campbell',2);
INSERT INTO Territories VALUES ('95054','Santa Clara',2);
INSERT INTO Territories VALUES ('95060','Santa Cruz',2);
INSERT INTO Territories VALUES ('98004','Bellevue',2);
INSERT INTO Territories VALUES ('98052','Redmond',2);
INSERT INTO Territories VALUES ('98104','Seattle',2);

-- EmployeeTerritories INSERT statements (49 rows)
;
INSERT INTO EmployeeTerritories VALUES (1,'06897');
INSERT INTO EmployeeTerritories VALUES (1,'19713');
INSERT INTO EmployeeTerritories VALUES (2,'01581');
INSERT INTO EmployeeTerritories VALUES (2,'01730');
INSERT INTO EmployeeTerritories VALUES (2,'01833');
INSERT INTO EmployeeTerritories VALUES (2,'02116');
INSERT INTO EmployeeTerritories VALUES (2,'02139');
INSERT INTO EmployeeTerritories VALUES (2,'02184');
INSERT INTO EmployeeTerritories VALUES (2,'40222');
INSERT INTO EmployeeTerritories VALUES (3,'30346');
INSERT INTO EmployeeTerritories VALUES (3,'31406');
INSERT INTO EmployeeTerritories VALUES (3,'32859');
INSERT INTO EmployeeTerritories VALUES (3,'33607');
INSERT INTO EmployeeTerritories VALUES (4,'20852');
INSERT INTO EmployeeTerritories VALUES (4,'27403');
INSERT INTO EmployeeTerritories VALUES (4,'27511');
INSERT INTO EmployeeTerritories VALUES (5,'02903');
INSERT INTO EmployeeTerritories VALUES (5,'07960');
INSERT INTO EmployeeTerritories VALUES (5,'08837');
INSERT INTO EmployeeTerritories VALUES (5,'10019');
INSERT INTO EmployeeTerritories VALUES (5,'10038');
INSERT INTO EmployeeTerritories VALUES (5,'11747');
INSERT INTO EmployeeTerritories VALUES (5,'14450');
INSERT INTO EmployeeTerritories VALUES (6,'85014');
INSERT INTO EmployeeTerritories VALUES (6,'85251');
INSERT INTO EmployeeTerritories VALUES (6,'98004');
INSERT INTO EmployeeTerritories VALUES (6,'98052');
INSERT INTO EmployeeTerritories VALUES (6,'98104');
INSERT INTO EmployeeTerritories VALUES (7,'60179');
INSERT INTO EmployeeTerritories VALUES (7,'60601');
INSERT INTO EmployeeTerritories VALUES (7,'80202');
INSERT INTO EmployeeTerritories VALUES (7,'80909');
INSERT INTO EmployeeTerritories VALUES (7,'90405');
INSERT INTO EmployeeTerritories VALUES (7,'94025');
INSERT INTO EmployeeTerritories VALUES (7,'94105');
INSERT INTO EmployeeTerritories VALUES (7,'95008');
INSERT INTO EmployeeTerritories VALUES (7,'95054');
INSERT INTO EmployeeTerritories VALUES (7,'95060');
INSERT INTO EmployeeTerritories VALUES (8,'19428');
INSERT INTO EmployeeTerritories VALUES (8,'44122');
INSERT INTO EmployeeTerritories VALUES (8,'45839');
INSERT INTO EmployeeTerritories VALUES (8,'53404');
INSERT INTO EmployeeTerritories VALUES (9,'03049');
INSERT INTO EmployeeTerritories VALUES (9,'03801');
INSERT INTO EmployeeTerritories VALUES (9,'48075');
INSERT INTO EmployeeTerritories VALUES (9,'48084');
INSERT INTO EmployeeTerritories VALUES (9,'48304');
INSERT INTO EmployeeTerritories VALUES (9,'55113');
INSERT INTO EmployeeTerritories VALUES (9,'55439');











-- INSERT statements
-- Reference: instnwnd.sql (SQL Server version)
-- Tables: Employees, Region, Territories, EmployeeTerritories

-- Drop existing tables
