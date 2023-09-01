CREATE TABLE "Project" (
    "Name" TEXT NOT NULL CONSTRAINT "PK_Project" PRIMARY KEY,
    "Description" TEXT NOT NULL
);


CREATE TABLE "Employee" (
    "Name" TEXT NOT NULL CONSTRAINT "PK_Employee" PRIMARY KEY,
    "Description" TEXT NOT NULL,
    "ProjectName" TEXT NULL,
    CONSTRAINT "FK_Employee_Project_ProjectName" FOREIGN KEY ("ProjectName") REFERENCES "Project" ("Name") ON DELETE SET NULL
);


CREATE INDEX "IX_Employee_ProjectName" ON "Employee" ("ProjectName");


