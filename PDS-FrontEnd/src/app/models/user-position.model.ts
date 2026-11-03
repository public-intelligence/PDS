export interface PersonnelDto {
  id: string;
  firstName: string;
  lastName: string;
  identificationNumber: string | null;
  gender: string | null;
  birthdate: string | null;
  email: string | null;
  phoneNumber: string | null;
  isExternal: boolean;
  profilePhotoId: string | null;
  status: string;
}

export interface OccupationalGroupDto {
  id: string;
  name: string;
}

export interface OrganizationalStructureDto {
  id: string;
  code: string;
  name: string;
}

export interface PositionDto {
  id: string;
  name: string;
  competencies: string | null;
  grade: number;
  code: number;
  minSalary: number;
  maxSalary: number;
  isManager: boolean;
  occupationalGroup: OccupationalGroupDto | null;
  organizationalStructure: OrganizationalStructureDto | null;
}

export interface UserPositionDto {
  personnelId: string;
  userId: string;
  email: string;
  userApplicationId: string;
  organizationApplicationId: number;
  createdAt: string;
  personnel: PersonnelDto | null;
  positionId: string;
  position: PositionDto | null;
  roleId: number;
  roleName: string;
}

export interface UserPositionPagedResultDto {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  succeeded: boolean;
  message: string | null;
  errors: string[] | null;
  data: UserPositionDto[];
}
