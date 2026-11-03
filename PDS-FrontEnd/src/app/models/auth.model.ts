export interface AuthTokenResponse {
  accessToken: string;
  mustChangePassword: boolean;
}

export interface RefreshTokenResponse {
  accessToken: string;
}

export interface JwtPayload {
  sub?: string;
  person_id?: string;
  application_id?: string;
  gobid_logo_url?: string;
  organization_application_id?: string;
  organization_id?: string;
  organization_name?: string;
  organization_acronym?: string;
  role_name?: string;
  user_application_id?: string;
  person_full_name?: string;
  permissions?: string[];
  session_id?: string;
  exp: number;
  iss?: string;
  aud?: string;
}
