import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Employee,
  AttendanceRecord,
  LeaveRequest,
  LeaveBalance,
  DashboardSummary,
  ClockInRequest,
  ClockOutRequest,
  ManualAttendanceRequest,
  CreateLeaveRequest,
  LeaveApprovalRequest,
  CompanyRegisterReq,
  LoginReq,
  AuthResponse
} from '../models/types';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private baseUrl = environment.apiUrl;
  private AUTH_KEY = 'attendance_company_session';

  constructor(private http: HttpClient) { }

  private getAuthHeaders(params?: HttpParams): { headers: HttpHeaders; params?: HttpParams } {
    const session = this.getAuthSession();
    let headers = new HttpHeaders();
    if (session?.token) {
      headers = headers.set('Authorization', `Bearer ${session.token}`);
    }
    if (session?.companyId) {
      headers = headers.set('X-Company-Id', session.companyId);
    }
    return params ? { headers, params } : { headers };
  }

  // Authentication & Company Registration
  registerCompany(req: CompanyRegisterReq): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/register-company`, req);
  }

  login(req: LoginReq): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/login`, req);
  }

  saveSession(session: AuthResponse): void {
    localStorage.setItem(this.AUTH_KEY, JSON.stringify(session));
  }

  getAuthSession(): AuthResponse | null {
    const raw = localStorage.getItem(this.AUTH_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      return null;
    }
  }

  logout(): void {
    localStorage.removeItem(this.AUTH_KEY);
  }

  isLoggedIn(): boolean {
    return !!this.getAuthSession();
  }

  // Employees
  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${this.baseUrl}/employees`, this.getAuthHeaders());
  }

  getEmployee(id: string | number): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/employees/${id}`, this.getAuthHeaders());
  }

  createEmployee(emp: Partial<Employee>): Observable<Employee> {
    return this.http.post<Employee>(`${this.baseUrl}/employees`, emp, this.getAuthHeaders());
  }

  updateEmployee(id: string | number, emp: Partial<Employee>): Observable<Employee> {
    return this.http.put<Employee>(`${this.baseUrl}/employees/${id}`, emp, this.getAuthHeaders());
  }

  deleteEmployee(id: string | number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/employees/${id}`, this.getAuthHeaders());
  }

  getLeaveBalance(employeeId: string | number): Observable<LeaveBalance> {
    return this.http.get<LeaveBalance>(`${this.baseUrl}/employees/${employeeId}/balance`, this.getAuthHeaders());
  }

  // Dashboard
  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard/summary`, this.getAuthHeaders());
  }

  // Attendance (Pointage)
  getAttendance(employeeId?: string | number, date?: string, department?: string): Observable<AttendanceRecord[]> {
    let params = new HttpParams();
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    if (date) params = params.set('date', date);
    if (department) params = params.set('department', department);

    return this.http.get<AttendanceRecord[]>(`${this.baseUrl}/attendance`, this.getAuthHeaders(params));
  }

  getTodayAttendance(employeeId: string | number): Observable<AttendanceRecord | null> {
    return this.http.get<AttendanceRecord | null>(`${this.baseUrl}/attendance/today/${employeeId}`, this.getAuthHeaders());
  }

  clockIn(req: ClockInRequest): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(`${this.baseUrl}/attendance/clock-in`, req, this.getAuthHeaders());
  }

  clockOut(req: ClockOutRequest): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(`${this.baseUrl}/attendance/clock-out`, req, this.getAuthHeaders());
  }

  addManualAttendance(req: ManualAttendanceRequest): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(`${this.baseUrl}/attendance/manual`, req, this.getAuthHeaders());
  }

  // Leaves (Congés)
  getLeaves(employeeId?: string | number, status?: string): Observable<LeaveRequest[]> {
    let params = new HttpParams();
    if (employeeId) params = params.set('employeeId', employeeId.toString());
    if (status) params = params.set('status', status);

    return this.http.get<LeaveRequest[]>(`${this.baseUrl}/leaves`, this.getAuthHeaders(params));
  }

  createLeave(req: CreateLeaveRequest): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.baseUrl}/leaves`, req, this.getAuthHeaders());
  }

  processLeaveApproval(id: string | number, req: LeaveApprovalRequest): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.baseUrl}/leaves/${id}/approval`, req, this.getAuthHeaders());
  }

  cancelLeave(id: string | number): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.baseUrl}/leaves/${id}/cancel`, {}, this.getAuthHeaders());
  }
}
